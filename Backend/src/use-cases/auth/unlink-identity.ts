import type { IdentityProvider } from '@/generated/prisma/client'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import { IdentityNotFoundError, LastIdentityError } from '@/use-cases/_errors/auth-errors'

export type UnlinkIdentityRequest = {
  userId: string
  provider: IdentityProvider
}

export class UnlinkIdentityUseCase {
  constructor(private readonly identities: UserIdentitiesRepository) {}

  async execute({ userId, provider }: UnlinkIdentityRequest): Promise<void> {
    const result = await this.identities.deleteUnlessLast(userId, provider)

    if (result === 'not_found') throw new IdentityNotFoundError()
    if (result === 'last_identity') throw new LastIdentityError()
  }
}
