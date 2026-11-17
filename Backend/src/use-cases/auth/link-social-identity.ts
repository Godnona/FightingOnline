import type { UserIdentity } from '@/generated/prisma/client'
import type { SocialProvider, SocialTokenVerifiers } from '@/lib/oauth/social-token-verifier'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import {
  EmailAlreadyInUseError,
  IdentityAlreadyLinkedError,
  ProviderAlreadyLinkedError,
} from '@/use-cases/_errors/auth-errors'

import { verifySocialToken } from './verify-social-token'

export type LinkSocialIdentityRequest = {
  userId: string
  provider: SocialProvider
  token: string
  providerGivenName?: string | null
  providerFamilyName?: string | null
}

export class LinkSocialIdentityUseCase {
  constructor(
    private readonly identities: UserIdentitiesRepository,
    private readonly verifiers: SocialTokenVerifiers,
  ) {}

  async execute(request: LinkSocialIdentityRequest): Promise<UserIdentity> {
    const { userId, provider, token } = request
    const profile = await verifySocialToken(this.verifiers, provider, token)

    const existing = await this.identities.findByProvider(provider, profile.providerUserId)
    if (existing) {
      if (existing.userId === userId) return existing
      throw new IdentityAlreadyLinkedError()
    }

    if (profile.email) {
      const owners = await this.identities.findManyByEmail(profile.email)
      if (owners.some((identity) => identity.userId !== userId)) throw new EmailAlreadyInUseError()
    }

    try {
      return await this.identities.create(userId, {
        provider,
        providerUserId: profile.providerUserId,
        email: profile.email,
        emailVerifiedAt: profile.email ? new Date() : null,
        providerGivenName: provider === 'APPLE' ? request.providerGivenName : undefined,
        providerFamilyName: provider === 'APPLE' ? request.providerFamilyName : undefined,
      })
    } catch (error) {
      if (!(error instanceof UniqueConstraintError)) throw error
      if (error.involves('providerUserId')) throw new IdentityAlreadyLinkedError()
      throw new ProviderAlreadyLinkedError()
    }
  }
}
