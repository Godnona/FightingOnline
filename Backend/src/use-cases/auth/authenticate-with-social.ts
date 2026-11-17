import type { User, UserIdentity } from '@/generated/prisma/client'
import type {
  SocialProfile,
  SocialProvider,
  SocialTokenVerifiers,
} from '@/lib/oauth/social-token-verifier'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import {
  AccountBannedError,
  DisplayNameTakenError,
  EmailAlreadyInUseError,
  InvalidCredentialsError,
} from '@/use-cases/_errors/auth-errors'

import type { DisplayNameGenerator } from './display-name-generator'
import type { AuthSession, SessionIssuer, SessionMetadata } from './session-issuer'
import { verifySocialToken } from './verify-social-token'

export type AuthenticateWithSocialRequest = SessionMetadata & {
  provider: SocialProvider
  token: string
}

export type AuthenticateWithSocialResponse = {
  user: User
  session: AuthSession
  isNewUser: boolean
}

const GENERATED_NAME_ATTEMPTS = 3

export class AuthenticateWithSocialUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly identities: UserIdentitiesRepository,
    private readonly verifiers: SocialTokenVerifiers,
    private readonly displayNames: DisplayNameGenerator,
    private readonly sessions: SessionIssuer,
  ) {}

  async execute(request: AuthenticateWithSocialRequest): Promise<AuthenticateWithSocialResponse> {
    const { provider } = request
    const profile = await verifySocialToken(this.verifiers, provider, request.token)

    const existing = await this.identities.findByProvider(provider, profile.providerUserId)
    if (existing) return this.signIn(existing, request)

    // never merge accounts by email: the player signs in to the old account and links this one
    if (profile.email && (await this.identities.findManyByEmail(profile.email)).length > 0) {
      throw new EmailAlreadyInUseError()
    }

    const user = await this.createUser(provider, profile)
    if (!user) {
      // another request created this identity in the meantime
      const created = await this.identities.findByProvider(provider, profile.providerUserId)
      if (!created) throw new Error('social identity vanished after a unique violation')
      return this.signIn(created, request)
    }

    const session = await this.sessions.start(user, request)
    return { user, session, isNewUser: true }
  }

  private async signIn(
    identity: UserIdentity,
    metadata: SessionMetadata,
  ): Promise<AuthenticateWithSocialResponse> {
    const user = await this.users.findById(identity.userId)
    if (!user || user.status === 'DELETED') throw new InvalidCredentialsError()
    if (user.status === 'BANNED') throw new AccountBannedError()

    await this.identities.update(identity.id, { lastUsedAt: new Date() })

    const session = await this.sessions.start(user, metadata)
    return { user, session, isNewUser: false }
  }

  private async createUser(provider: SocialProvider, profile: SocialProfile): Promise<User | null> {
    for (let attempt = 1; ; attempt++) {
      try {
        const { identities: _identities, ...user } = await this.users.create({
          displayName: await this.displayNames.generate(),
          identity: {
            provider,
            providerUserId: profile.providerUserId,
            email: profile.email,
            emailVerifiedAt: profile.email ? new Date() : null,
          },
        })
        return user
      } catch (error) {
        if (!(error instanceof UniqueConstraintError)) throw error
        if (error.involves('providerUserId')) return null
        if (attempt >= GENERATED_NAME_ATTEMPTS) throw new DisplayNameTakenError()
      }
    }
  }
}
