import { socialTokenVerifiers } from '@/lib/oauth'
import { AuthenticateWithSocialUseCase } from '@/use-cases/auth/authenticate-with-social'
import { DisplayNameGenerator } from '@/use-cases/auth/display-name-generator'

import { makeUserIdentitiesRepository, makeUsersRepository } from './make-repositories'
import { makeSessionIssuer } from './make-session-issuer'

export function makeAuthenticateWithSocialUseCase() {
  const users = makeUsersRepository()
  return new AuthenticateWithSocialUseCase(
    users,
    makeUserIdentitiesRepository(),
    socialTokenVerifiers,
    new DisplayNameGenerator(users),
    makeSessionIssuer(),
  )
}
