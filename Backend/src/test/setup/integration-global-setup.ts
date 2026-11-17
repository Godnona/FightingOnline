import { execFileSync } from 'node:child_process'
import { createRequire } from 'node:module'
import { dirname, join } from 'node:path'

export default function setup() {
  const databaseUrl = process.env.DATABASE_URL_TEST

  if (!databaseUrl) {
    throw new Error('DATABASE_URL_TEST is not set')
  }

  const databaseName = new URL(databaseUrl).pathname.replace(/^\//, '')

  if (!databaseName.endsWith('_test')) {
    throw new Error(
      `Refusing to run integration tests against "${databaseName}": the database name must end with "_test"`,
    )
  }

  const require = createRequire(import.meta.url)
  const prismaCli = join(dirname(require.resolve('prisma/package.json')), 'build', 'index.js')

  execFileSync(process.execPath, [prismaCli, 'db', 'push', '--skip-generate'], {
    env: { ...process.env, DATABASE_URL: databaseUrl },
    stdio: 'pipe',
  })
}
