[CmdletBinding()]
param(
    [ValidateRange(100, 1800)]
    [int]$BatchSizeMiB = 900,

    [ValidateRange(0, 300)]
    [int]$PauseSeconds = 12,

    [string]$Remote = "origin",
    [string]$Branch = "main",

    [switch]$Execute
)

$ErrorActionPreference = "Stop"
$repoRoot = (git rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
    throw "Run this script from inside a Git repository."
}

Set-Location -LiteralPath $repoRoot

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Format-Size {
    param([long]$Bytes)
    return "{0:N1} MiB" -f ($Bytes / 1MB)
}

$currentBranch = (git branch --show-current).Trim()
if ($currentBranch -ne $Branch) {
    throw "Current branch is '$currentBranch'; expected '$Branch'."
}

if (-not (git remote | Where-Object { $_ -eq $Remote })) {
    throw "Remote '$Remote' does not exist."
}

$trackedChanges = @(git status --porcelain=v1 --untracked-files=no)
if ($trackedChanges.Count -gt 0) {
    throw "Tracked files already have changes. Commit or stash them before running this script."
}

$remoteRef = "refs/remotes/$Remote/$Branch"
& git show-ref --verify --quiet $remoteRef
if ($LASTEXITCODE -ne 0) {
    throw "Remote-tracking branch '$Remote/$Branch' is missing. Run: git fetch $Remote $Branch"
}

& git merge-base --is-ancestor "$Remote/$Branch" HEAD
if ($LASTEXITCODE -ne 0) {
    throw "'$Remote/$Branch' is not an ancestor of HEAD. Resolve the branch divergence first."
}

# Resume safely if an earlier run committed a batch but its push failed.
$pendingCommits = @(git rev-list --reverse "$Remote/$Branch..HEAD")
if ($pendingCommits.Count -gt 0) {
    Write-Host "Found $($pendingCommits.Count) local commit(s) not recorded in $Remote/$Branch."
    foreach ($sha in $pendingCommits) {
        $pushRef = "${sha}:refs/heads/$Branch"
        if (-not $Execute) {
            Write-Host "  git push $Remote $pushRef"
            continue
        }

        Invoke-Git push $Remote $pushRef
        Invoke-Git update-ref $remoteRef $sha
        if ($PauseSeconds -gt 0) {
            Start-Sleep -Seconds $PauseSeconds
        }
    }
}

$rawUntracked = (& git ls-files --others --exclude-standard -z)
if ($LASTEXITCODE -ne 0) {
    throw "Could not list untracked files."
}

$paths = @($rawUntracked -split "`0" | Where-Object { $_ })
if ($paths.Count -eq 0) {
    Write-Host "No untracked files remain."
    exit 0
}

$pathSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($path in $paths) {
    [void]$pathSet.Add($path)
}

# Keep a Unity asset and its .meta file in the same commit.
$unitsByKey = @{}
foreach ($path in $paths) {
    $key = $path
    if ($path.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
        $assetPath = $path.Substring(0, $path.Length - 5)
        if ($pathSet.Contains($assetPath)) {
            $key = $assetPath
        }
    }
    elseif ($pathSet.Contains("$path.meta")) {
        $key = $path
    }

    if (-not $unitsByKey.ContainsKey($key)) {
        $unitsByKey[$key] = [System.Collections.Generic.List[string]]::new()
    }
    $unitsByKey[$key].Add($path)
}

$githubObjectLimit = 100MB
$units = foreach ($entry in $unitsByKey.GetEnumerator()) {
    $unitPaths = @($entry.Value)
    $size = 0L
    foreach ($path in $unitPaths) {
        $file = Get-Item -LiteralPath $path
        if ($file.Length -ge $githubObjectLimit) {
            throw "'$path' is $(Format-Size $file.Length), which reaches GitHub's 100 MiB regular-Git limit. Use Git LFS or reduce the file first."
        }
        $size += $file.Length
    }

    [pscustomobject]@{
        Key   = $entry.Key
        Paths = $unitPaths
        Size  = $size
    }
}

$batchLimit = [long]$BatchSizeMiB * 1MB
$batches = [System.Collections.Generic.List[object]]::new()
$currentPaths = [System.Collections.Generic.List[string]]::new()
$currentSize = 0L

foreach ($unit in @($units | Sort-Object Key)) {
    if ($unit.Size -gt $batchLimit) {
        throw "Asset group '$($unit.Key)' is $(Format-Size $unit.Size), larger than the configured batch size."
    }

    if ($currentPaths.Count -gt 0 -and ($currentSize + $unit.Size) -gt $batchLimit) {
        $batches.Add([pscustomobject]@{ Paths = @($currentPaths); Size = $currentSize })
        $currentPaths = [System.Collections.Generic.List[string]]::new()
        $currentSize = 0L
    }

    foreach ($path in $unit.Paths) {
        $currentPaths.Add($path)
    }
    $currentSize += $unit.Size
}

if ($currentPaths.Count -gt 0) {
    $batches.Add([pscustomobject]@{ Paths = @($currentPaths); Size = $currentSize })
}

$totalSize = ($batches | Measure-Object -Property Size -Sum).Sum
Write-Host "Planned $($batches.Count) batch(es), $($paths.Count) file(s), $(Format-Size $totalSize) total."

for ($index = 0; $index -lt $batches.Count; $index++) {
    $batch = $batches[$index]
    $number = $index + 1
    Write-Host ("Batch {0}/{1}: {2} files, {3}" -f $number, $batches.Count, $batch.Paths.Count, (Format-Size $batch.Size))

    if (-not $Execute) {
        Write-Host ("  git add -- <{0} files in batch {1}>" -f $batch.Paths.Count, $number)
        Write-Host ("  git commit -m `"Import project files (batch {0}/{1})`"" -f $number, $batches.Count)
        Write-Host "  git push $Remote <commit-sha>:refs/heads/$Branch"
        continue
    }

    $pathspecFile = [System.IO.Path]::GetTempFileName()
    try {
        $pathspec = ($batch.Paths -join "`0") + "`0"
        [System.IO.File]::WriteAllText(
            $pathspecFile,
            $pathspec,
            [System.Text.UTF8Encoding]::new($false)
        )

        Invoke-Git add "--pathspec-from-file=$pathspecFile" --pathspec-file-nul
        & git diff --cached --quiet
        if ($LASTEXITCODE -eq 0) {
            throw "Batch $number did not stage any changes."
        }

        Invoke-Git commit -m "Import project files (batch $number/$($batches.Count))"
        $sha = (git rev-parse HEAD).Trim()
        $pushRef = "${sha}:refs/heads/$Branch"
        Invoke-Git push $Remote $pushRef
        Invoke-Git update-ref $remoteRef $sha
    }
    finally {
        Remove-Item -LiteralPath $pathspecFile -Force -ErrorAction SilentlyContinue
    }

    if ($PauseSeconds -gt 0 -and $number -lt $batches.Count) {
        Start-Sleep -Seconds $PauseSeconds
    }
}

if (-not $Execute) {
    Write-Host "Preview only. Re-run with -Execute to commit and push each batch."
    exit 0
}

Write-Host "All batches were committed and pushed successfully."
