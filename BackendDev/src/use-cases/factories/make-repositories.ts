import { prisma } from '@/lib/prisma'
import { PrismaPasswordResetTokensRepository } from '@/repositories/prisma/prisma-password-reset-tokens-repository'
import { PrismaRefreshTokensRepository } from '@/repositories/prisma/prisma-refresh-tokens-repository'
import { PrismaUserIdentitiesRepository } from '@/repositories/prisma/prisma-user-identities-repository'
import { PrismaUsersRepository } from '@/repositories/prisma/prisma-users-repository'

export const makeUsersRepository = () => new PrismaUsersRepository(prisma)
export const makeUserIdentitiesRepository = () => new PrismaUserIdentitiesRepository(prisma)
export const makeRefreshTokensRepository = () => new PrismaRefreshTokensRepository(prisma)
export const makePasswordResetTokensRepository = () =>
  new PrismaPasswordResetTokensRepository(prisma)
