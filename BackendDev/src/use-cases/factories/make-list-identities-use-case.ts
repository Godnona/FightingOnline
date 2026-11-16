import { ListIdentitiesUseCase } from '@/use-cases/auth/list-identities'

import { makeUserIdentitiesRepository } from './make-repositories'

export function makeListIdentitiesUseCase() {
  return new ListIdentitiesUseCase(makeUserIdentitiesRepository())
}
