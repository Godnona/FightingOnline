import { env } from '@/env'
import { UpdateProfileUseCase } from '@/use-cases/profile/update-profile'

import { makeUsersRepository } from './make-repositories'

export function makeUpdateProfileUseCase() {
  return new UpdateProfileUseCase(makeUsersRepository(), env.AVATAR_IDS)
}
