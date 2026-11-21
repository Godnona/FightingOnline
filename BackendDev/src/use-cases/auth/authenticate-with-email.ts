import type { User } from '@/generated/prisma/client'
import type { PasswordHasher } from '@/lib/password-hasher'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import { AccountBannedError, InvalidCredentialsError } from '@/use-cases/_errors/auth-errors'
import { normalizeEmail } from '@/utils/email'

import type { AuthSession, SessionIssuer, SessionMetadata } from './session-issuer'

export type AuthenticateWithEmailRequest = SessionMetadata & {
  email: string
  password: string
}

export type AuthenticateWithEmailResponse = {
  user: User
  session: AuthSession
}

export class AuthenticateWithEmailUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly identities: UserIdentitiesRepository,
    private readonly passwordHasher: PasswordHasher,
    private readonly sessions: SessionIssuer,
  ) {}

  async execute(request: AuthenticateWithEmailRequest): Promise<AuthenticateWithEmailResponse> {
    const identity = await this.identities.findByProvider('EMAIL', normalizeEmail(request.email))

    if (!identity?.passwordHash) {
      // same cost as a real check, otherwise response time reveals which emails exist
      await this.passwordHasher.verifyAgainstDummy(request.password)
      throw new InvalidCredentialsError()
    }

    const passwordMatches = await this.passwordHasher.verify(
      identity.passwordHash,
      request.password,
    )
    if (!passwordMatches) throw new InvalidCredentialsError()

    const user = await this.users.findById(identity.userId)
    if (!user || user.status === 'DELETED') throw new InvalidCredentialsError()
    if (user.status === 'BANNED') throw new AccountBannedError()

    await this.identities.update(identity.id, {
      lastUsedAt: new Date(),
      ...(this.passwordHasher.needsRehash(identity.passwordHash) && {
        passwordHash: await this.passwordHasher.hash(request.password),
      }),
    })

    const session = await this.sessions.start(user, request)
    return { user, session }
  }
}
