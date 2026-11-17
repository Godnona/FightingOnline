import { UnlinkIdentityUseCase } from '@/use-cases/auth/unlink-identity'

import { makeUserIdentitiesRepository } from './make-repositories'

export function makeUnlinkIdentityUseCase() {
  return new UnlinkIdentityUseCase(makeUserIdentitiesRepository())
}
