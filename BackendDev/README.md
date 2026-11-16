# ripptopia-api

Backend for the game: auth, player profiles, social, matchmaking and the game server integration.

Client integration docs: [docs/integration-guide.md](docs/integration-guide.md)

## Running locally

Needs Node 22+ and Docker.

```bash
npm install
cp .env.example .env
npm run db:up
npm run db:migrate
npm run dev
```

The API runs on http://localhost:3333 (`GET /health/ready` checks the database too).

## Scripts

- `npm run dev` - dev server with reload
- `npm run build` / `npm start` - production build
- `npm run check` - typecheck, lint, format and tests
- `npm test` - unit and integration tests (needs `npm run db:up`)
- `npm run db:migrate` - create and apply migrations
- `npm run db:deploy` - apply migrations on staging/production
- `npm run db:studio` - Prisma Studio
- `npm run user:set-role -- <email | display name | user id> <PLAYER | ADMIN>` - change a player's role. Use it for the first admin; after that admins can use `PATCH /admin/users/:userId/role`

Environment variables are listed in `.env.example`.
