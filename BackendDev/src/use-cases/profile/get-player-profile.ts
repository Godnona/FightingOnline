import type { User } from '@/generated/prisma/client'
import type { UsersRepository } from '@/repositories/users-repository'
import { PlayerNotFoundError } from '@/use-cases/_errors/profile-errors'

/** what any signed-in player can see about another one */
export class GetPlayerProfileUseCase {
  constructor(private readonly users: UsersRepository) {}

  async execute({ playerId }: { playerId: string }): Promise<User> {
    const user = await this.users.findById(playerId)
    if (!user || user.status === 'DELETED') throw new PlayerNotFoundError()
    return user
  }
}
