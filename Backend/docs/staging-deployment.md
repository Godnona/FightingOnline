# Staging deployment through GitHub

The repository includes:

- `.github/workflows/backend-ci.yml`: typecheck, lint, format, MongoDB integration tests and build.
- root `render.yaml`: a Render Blueprint for the API under `Backend/`.
- `Backend/Dockerfile`: an alternative for any container host.

MongoDB Atlas is recommended for staging. The API needs a replica set because account creation,
identity unlinking and refresh-token rotation use transactions. Atlas provides this by default.

## Information the owner must provide

### Infrastructure

- GitHub repository URL and permission to push or open a pull request.
- Render account/workspace (or the chosen cloud) and staging region.
- MongoDB Atlas `DATABASE_URL` for a staging database, with the Render outbound addresses allowed
  in Atlas Network Access. Use a separate database name such as `ripptopia_staging`.
- Staging API hostname, for example `api-staging.example.com`, and DNS access if a custom domain is
  required.

### Application and providers

- A random `JWT_SECRET` of at least 32 characters (Render can generate it).
- Google OAuth client IDs used by Android, iOS and PC/Web.
- Facebook App ID and App Secret; the app must be in Active mode for real-player testing.
- Android package name and SHA-1 fingerprints for debug and release certificates.
- SMTP host, port, TLS mode, username/password and a verified `MAIL_FROM` address.
- Public privacy-policy URL, support email, game/company name and logo for provider consent screens.
- Unity login plugin choice and confirmation that it sends Google ID tokens and Facebook access or
  Limited Login tokens.

## First deployment

1. Push this project to GitHub. Keep `Backend/.env` out of Git; only `.env.example` is committed.
2. In Render, choose **New > Blueprint**, connect the GitHub repository and select `render.yaml`.
3. Fill every environment variable marked `sync: false` in the Blueprint. Never place provider or
   database secrets in GitHub files.
4. Deploy and wait for `GET /health/ready` to return `200` with `database: up`.
5. Open `/docs`, then test email register/login, password reset, Google, Facebook, identity
   link/unlink and profile routes.
6. Set Unity's Staging URL to the HTTPS service URL with no trailing path. Do not use the local
   MongoDB URL or expose MongoDB to the game client.

## GitHub-triggered Render deploy

The Blueprint uses `autoDeployTrigger: checksPass`. Render watches the linked GitHub branch and
deploys only after the GitHub Actions checks pass. No deploy credential is committed to the repo.
Select `main` as the Blueprint branch and keep branch protection enabled so failed checks cannot be
merged.

## Git commands

```bash
git checkout -b codex/mongodb-backend-week3
git add Backend .github/workflows/backend-ci.yml render.yaml README.md Assets/APIClient/README.md
git commit -m "build backend staging milestone on MongoDB"
git push -u origin codex/mongodb-backend-week3
```

Open a pull request into `main`. After CI passes, merge it; Render then deploys the new commit.

## Staging acceptance checklist

- `/health` and `/health/ready` return 200; every response has `x-request-id`.
- `/docs/json` contains auth, identity, profile and admin routes.
- Email reset arrives through SMTP and a code can be used only once.
- Refresh-token reuse ends the token family; logout-all ends every player session.
- Google and Facebook tokens are checked against the configured app/client IDs.
- Linking never merges accounts by email and the last identity cannot be removed.
- Unity handles the documented error `code` values and retries once after `ACCESS_TOKEN_EXPIRED`.
