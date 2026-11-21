import type { Role, User } from '@/generated/prisma/client'
import type { UsersRepository } from '@/repositories/users-repository'
import { CannotChangeOwnRoleError, PlayerNotFoundError } from '@/use-cases/_errors/profile-errors'

export type SetUserRoleRequest = {
  userId: string
  role: Role
  /** the admin doing it, null when it comes from the command line */
  actorId: string | null
}

export class SetUserRoleUseCase {
  constructor(private readonly users: UsersRepository) {}

  async execute({ userId, role, actorId }: SetUserRoleRequest): Promise<User> {
    // keeps an admin from locking themselves out by accident
    if (actorId === userId) throw new CannotChangeOwnRoleError()

    const user = await this.users.findById(userId)
    if (!user || user.status === 'DELETED') throw new PlayerNotFoundError()
    if (user.role === role) return user

    return this.users.update(userId, { role })
  }
}
