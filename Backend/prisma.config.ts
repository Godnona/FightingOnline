import { defineConfig } from 'prisma/config'

// prisma cli doesn't read .env on its own
try {
  process.loadEnvFile()
} catch {
  // no .env (ci), use the process env
}

export default defineConfig({
  schema: 'prisma/schema.prisma',
  datasource: {
    url:
      process.env.DATABASE_URL ??
      'mongodb://127.0.0.1:27017/ripptopia?replicaSet=rs0&directConnection=true',
  },
})
