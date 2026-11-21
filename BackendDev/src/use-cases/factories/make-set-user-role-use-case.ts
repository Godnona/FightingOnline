import { SetUserRoleUseCase } from '@/use-cases/admin/set-user-role'

import { makeUsersRepository } from './make-repositories'

export function makeSetUserRoleUseCase() {
  return new SetUserRoleUseCase(makeUsersRepository())
}
