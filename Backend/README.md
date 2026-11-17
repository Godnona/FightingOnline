# ripptopia-api

Backend for the game: auth, player profiles, social, matchmaking and the game server integration.

Client integration docs: [docs/integration-guide.md](docs/integration-guide.md)

Staging and GitHub deployment: [docs/staging-deployment.md](docs/staging-deployment.md)

## Running locally

Needs Node 22+ and Docker. Local MongoDB runs as a single-node replica set because auth
flows use transactions.

```bash
npm install
cp .env.example .env
npm run db:up
npm run db:migrate
npm run dev
```

The API runs on http://localhost:3333 (`GET /health/ready` checks the database too).
Swagger UI runs on http://localhost:3333/docs.

## Scripts

- `npm run dev` - dev server with reload
- `npm run build` / `npm start` - production build
- `npm run check` - typecheck, lint, format and tests
- `npm test` - unit and integration tests (needs `npm run db:up`)
- `npm run db:migrate` - push the Prisma schema and MongoDB indexes locally
- `npm run db:deploy` - push the Prisma schema and MongoDB indexes on staging/production
- `npm run db:studio` - Prisma Studio
- `npm run user:set-role -- <email | display name | user id> <PLAYER | ADMIN>` - change a player's role. Use it for the first admin; after that admins can use `PATCH /admin/users/:userId/role`

Environment variables are listed in `.env.example`.

MongoDB does not use Prisma Migrate migration files. Both database scripts use `prisma db push`;
review schema changes before running `db:deploy` against production. MongoDB Atlas already runs
as a replica set. A self-hosted production database must also be configured as one.
