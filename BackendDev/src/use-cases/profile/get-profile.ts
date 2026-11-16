import type { User } from '@/generated/prisma/client'
import type { UsersRepository } from '@/repositories/users-repository'
import { PlayerNotFoundError } from '@/use-cases/_errors/profile-errors'

export class GetProfileUseCase {
  constructor(private readonly users: UsersRepository) {}

  async execute({ userId }: { userId: string }): Promise<User> {
    const user = await this.users.findById(userId)
    if (!user) throw new PlayerNotFoundError()
    return user
  }
}
