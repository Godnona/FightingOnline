import { socialTokenVerifiers } from '@/lib/oauth'
import { LinkSocialIdentityUseCase } from '@/use-cases/auth/link-social-identity'

import { makeUserIdentitiesRepository } from './make-repositories'

export function makeLinkSocialIdentityUseCase() {
  return new LinkSocialIdentityUseCase(makeUserIdentitiesRepository(), socialTokenVerifiers)
}
