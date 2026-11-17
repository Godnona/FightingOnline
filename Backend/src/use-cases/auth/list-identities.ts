import type { UserIdentity } from '@/generated/prisma/client'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'

export class ListIdentitiesUseCase {
  constructor(private readonly identities: UserIdentitiesRepository) {}

  execute({ userId }: { userId: string }): Promise<UserIdentity[]> {
    return this.identities.findManyByUserId(userId)
  }
}
