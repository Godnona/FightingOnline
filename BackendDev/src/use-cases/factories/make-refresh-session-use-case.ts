import { logger } from '@/lib/logger'
import { RefreshSessionUseCase } from '@/use-cases/auth/refresh-session'

import { makeRefreshTokensRepository, makeUsersRepository } from './make-repositories'
import { makeSessionIssuer } from './make-session-issuer'

export function makeRefreshSessionUseCase() {
  return new RefreshSessionUseCase(
    makeRefreshTokensRepository(),
    makeUsersRepository(),
    makeSessionIssuer(),
    logger.child({ module: 'auth' }),
  )
}
