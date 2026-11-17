import { afterAll, describe } from 'vitest'

import { prisma } from '@/lib/prisma'
import { PrismaPasswordResetTokensRepository } from '@/repositories/prisma/prisma-password-reset-tokens-repository'
import { PrismaRefreshTokensRepository } from '@/repositories/prisma/prisma-refresh-tokens-repository'
import { PrismaUserIdentitiesRepository } from '@/repositories/prisma/prisma-user-identities-repository'
import { PrismaUsersRepository } from '@/repositories/prisma/prisma-users-repository'
import { describeRepositoriesContract } from '@/test/contracts/repositories.contract'
import { resetDatabase } from '@/test/helpers/reset-database'

describe('prisma repositories', () => {
  afterAll(async () => {
    await prisma.$disconnect()
  })

  describeRepositoriesContract(async () => {
    await resetDatabase(prisma)
    return {
      users: new PrismaUsersRepository(prisma),
      identities: new PrismaUserIdentitiesRepository(prisma),
      refreshTokens: new PrismaRefreshTokensRepository(prisma),
      passwordResetTokens: new PrismaPasswordResetTokensRepository(prisma),
    }
  })
})
