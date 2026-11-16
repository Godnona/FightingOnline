import type { PrismaClient } from '@/generated/prisma/client'

export async function resetDatabase(db: PrismaClient) {
  const [{ name }] = await db.$queryRaw<[{ name: string }]>`SELECT current_database() AS name`

  if (!name.endsWith('_test')) {
    throw new Error(`Refusing to truncate "${name}": not a test database`)
  }

  const rows = await db.$queryRaw<{ tablename: string }[]>`
    SELECT tablename FROM pg_tables
    WHERE schemaname = 'public' AND tablename <> '_prisma_migrations'
  `

  if (rows.length === 0) return

  const tables = rows.map(({ tablename }) => `"public"."${tablename}"`).join(', ')
  await db.$executeRawUnsafe(`TRUNCATE TABLE ${tables} RESTART IDENTITY CASCADE`)
}
