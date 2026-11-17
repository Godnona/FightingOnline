import { describe } from 'vitest'

import { describeRepositoriesContract } from '@/test/contracts/repositories.contract'
import { InMemoryDatabase } from '@/test/helpers/in-memory/in-memory-database'
import { InMemoryPasswordResetTokensRepository } from '@/test/helpers/in-memory/in-memory-password-reset-tokens-repository'
import { InMemoryRefreshTokensRepository } from '@/test/helpers/in-memory/in-memory-refresh-tokens-repository'
import { InMemoryUserIdentitiesRepository } from '@/test/helpers/in-memory/in-memory-user-identities-repository'
import { InMemoryUsersRepository } from '@/test/helpers/in-memory/in-memory-users-repository'

describe('in-memory repositories', () => {
  describeRepositoriesContract(async () => {
    const db = new InMemoryDatabase()
    return {
      users: new InMemoryUsersRepository(db),
      identities: new InMemoryUserIdentitiesRepository(db),
      refreshTokens: new InMemoryRefreshTokensRepository(db),
      passwordResetTokens: new InMemoryPasswordResetTokensRepository(db),
    }
  })
})
