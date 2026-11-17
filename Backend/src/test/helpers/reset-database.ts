import type { PrismaClient } from '@/generated/prisma/client'

export async function resetDatabase(db: PrismaClient) {
  const databaseUrl = process.env.DATABASE_URL
  if (!databaseUrl) throw new Error('DATABASE_URL is not set')

  const name = new URL(databaseUrl).pathname.replace(/^\//, '')

  if (!name.endsWith('_test')) {
    throw new Error(`Refusing to clear "${name}": not a test database`)
  }

  await db.passwordResetToken.deleteMany()
  await db.refreshToken.deleteMany()
  await db.userIdentity.deleteMany()
  await db.user.deleteMany()
}
