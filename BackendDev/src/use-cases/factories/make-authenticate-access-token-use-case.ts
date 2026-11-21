import { accessTokenService } from '@/lib/auth-services'
import { AuthenticateAccessTokenUseCase } from '@/use-cases/auth/authenticate-access-token'

import { makeUsersRepository } from './make-repositories'

export function makeAuthenticateAccessTokenUseCase() {
  return new AuthenticateAccessTokenUseCase(accessTokenService, makeUsersRepository())
}
