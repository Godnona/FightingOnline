import type { User } from '@/generated/prisma/client'
import type { PasswordHasher } from '@/lib/password-hasher'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { CreateIdentityData, UsersRepository } from '@/repositories/users-repository'
import { DisplayNameTakenError, EmailAlreadyInUseError } from '@/use-cases/_errors/auth-errors'
import { normalizeEmail } from '@/utils/email'

import type { DisplayNameGenerator } from './display-name-generator'
import type { AuthSession, SessionIssuer, SessionMetadata } from './session-issuer'

export type RegisterWithEmailRequest = SessionMetadata & {
  email: string
  password: string
  displayName?: string | null
}

export type RegisterWithEmailResponse = {
  user: User
  session: AuthSession
}

const GENERATED_NAME_ATTEMPTS = 3

export class RegisterWithEmailUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly identities: UserIdentitiesRepository,
    private readonly passwordHasher: PasswordHasher,
    private readonly displayNames: DisplayNameGenerator,
    private readonly sessions: SessionIssuer,
  ) {}

  async execute(request: RegisterWithEmailRequest): Promise<RegisterWithEmailResponse> {
    const email = normalizeEmail(request.email)

    // any provider counts: a social account with this email has to be linked, not duplicated
    if ((await this.identities.findManyByEmail(email)).length > 0) {
      throw new EmailAlreadyInUseError()
    }

    if (request.displayName && (await this.users.findByDisplayName(request.displayName))) {
      throw new DisplayNameTakenError()
    }

    const identity: CreateIdentityData = {
      provider: 'EMAIL',
      providerUserId: email,
      email,
      passwordHash: await this.passwordHasher.hash(request.password),
    }

    const user = await this.createUser(request.displayName ?? null, identity)
    const session = await this.sessions.start(user, request)

    const { identities: _identities, ...userWithoutIdentities } = user
    return { user: userWithoutIdentities, session }
  }

  private async createUser(chosenName: string | null, identity: CreateIdentityData) {
    for (let attempt = 1; ; attempt++) {
      const displayName = chosenName ?? (await this.displayNames.generate())

      try {
        return await this.users.create({ displayName, identity })
      } catch (error) {
        if (!(error instanceof UniqueConstraintError)) throw error

        if (!error.involves('displayName')) throw new EmailAlreadyInUseError()
        if (chosenName || attempt >= GENERATED_NAME_ATTEMPTS) throw new DisplayNameTakenError()
      }
    }
  }
}
