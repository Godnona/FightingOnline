import { GetPlayerProfileUseCase } from '@/use-cases/profile/get-player-profile'

import { makeUsersRepository } from './make-repositories'

export function makeGetPlayerProfileUseCase() {
  return new GetPlayerProfileUseCase(makeUsersRepository())
}
