import { GetProfileUseCase } from '@/use-cases/profile/get-profile'

import { makeUsersRepository } from './make-repositories'

export function makeGetProfileUseCase() {
  return new GetProfileUseCase(makeUsersRepository())
}
