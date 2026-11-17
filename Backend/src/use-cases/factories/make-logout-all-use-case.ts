import { LogoutAllUseCase } from '@/use-cases/auth/logout-all'

import { makeRefreshTokensRepository } from './make-repositories'

export function makeLogoutAllUseCase() {
  return new LogoutAllUseCase(makeRefreshTokensRepository())
}
