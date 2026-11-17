import { passwordHasher } from '@/lib/auth-services'
import { DisplayNameGenerator } from '@/use-cases/auth/display-name-generator'
import { RegisterWithEmailUseCase } from '@/use-cases/auth/register-with-email'

import { makeUserIdentitiesRepository, makeUsersRepository } from './make-repositories'
import { makeSessionIssuer } from './make-session-issuer'

export function makeRegisterWithEmailUseCase() {
  const users = makeUsersRepository()
  return new RegisterWithEmailUseCase(
    users,
    makeUserIdentitiesRepository(),
    passwordHasher,
    new DisplayNameGenerator(users),
    makeSessionIssuer(),
  )
}
