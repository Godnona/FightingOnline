import type { User } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type { UsersRepository } from '@/repositories/users-repository'
import { DisplayNameTakenError } from '@/use-cases/_errors/auth-errors'
import {
  DisplayNameChangeTooSoonError,
  InvalidAvatarError,
  PlayerNotFoundError,
} from '@/use-cases/_errors/profile-errors'

import { nextDisplayNameChangeAt, renameCooldownStart } from './display-name-policy'

export type UpdateProfileRequest = {
  userId: string
  displayName?: string
  /** null removes the avatar */
  avatar?: string | null
}

export class UpdateProfileUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly avatarIds: readonly string[],
  ) {}

  async execute(request: UpdateProfileRequest): Promise<User> {
    let user = await this.users.findById(request.userId)
    if (!user) throw new PlayerNotFoundError()

    const { avatar } = request
    if (avatar && !this.avatarIds.includes(avatar)) throw new InvalidAvatarError()

    // sending the current name again is not a rename and doesn't start the cooldown
    if (request.displayName !== undefined && request.displayName !== user.displayName) {
      user = await this.rename(user, request.displayName)
    }

    if (avatar !== undefined && avatar !== user.avatar) {
      user = await this.users.update(user.id, { avatar })
    }

    return user
  }

  private async rename(user: User, displayName: string): Promise<User> {
    const nextChangeAt = nextDisplayNameChangeAt(user)
    if (nextChangeAt) throw new DisplayNameChangeTooSoonError(nextChangeAt)

    const owner = await this.users.findByDisplayName(displayName)
    if (owner && owner.id !== user.id) throw new DisplayNameTakenError()

    try {
      const renamed = await this.users.rename(user.id, displayName, renameCooldownStart())
      if (renamed) return renamed
    } catch (error) {
      if (error instanceof UniqueConstraintError) throw new DisplayNameTakenError()
      throw error
    }

    // another request renamed the player in the meantime
    const current = await this.users.findById(user.id)
    throw new DisplayNameChangeTooSoonError(
      (current && nextDisplayNameChangeAt(current)) ?? new Date(),
    )
  }
}
