import { defineConfig } from 'prisma/config'

// prisma cli doesn't read .env on its own
try {
  process.loadEnvFile()
} catch {
  // no .env (ci), use the process env
}

export default defineConfig({
  schema: 'prisma/schema.prisma',
  migrations: {
    path: 'prisma/migrations',
  },
  datasource: {
    url: process.env.DIRECT_URL ?? process.env.DATABASE_URL,
  },
})
