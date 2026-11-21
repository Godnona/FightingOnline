import { LogoutUseCase } from '@/use-cases/auth/logout'

import { makeRefreshTokensRepository } from './make-repositories'

export function makeLogoutUseCase() {
  return new LogoutUseCase(makeRefreshTokensRepository())
}
