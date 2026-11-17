import { env } from '@/env'
import { prisma } from '@/lib/prisma'

async function main() {
  if (env.NODE_ENV === 'production') {
    throw new Error('Refusing to reset the database in production')
  }

  const databaseName = new URL(env.DATABASE_URL).pathname.replace(/^\//, '')
  if (!databaseName) throw new Error('DATABASE_URL must include a database name')

  await prisma.passwordResetToken.deleteMany()
  await prisma.refreshToken.deleteMany()
  await prisma.userIdentity.deleteMany()
  await prisma.user.deleteMany()
  process.stdout.write(`Cleared application data from MongoDB database "${databaseName}"\n`)
}

try {
  await main()
} finally {
  await prisma.$disconnect()
}
