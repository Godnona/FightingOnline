import { passwordHasher } from '@/lib/auth-services'
import { AuthenticateWithEmailUseCase } from '@/use-cases/auth/authenticate-with-email'

import { makeUserIdentitiesRepository, makeUsersRepository } from './make-repositories'
import { makeSessionIssuer } from './make-session-issuer'

export function makeAuthenticateWithEmailUseCase() {
  return new AuthenticateWithEmailUseCase(
    makeUsersRepository(),
    makeUserIdentitiesRepository(),
    passwordHasher,
    makeSessionIssuer(),
  )
}
