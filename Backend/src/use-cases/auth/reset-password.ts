import { timingSafeEqual } from 'node:crypto'

import type { PasswordHasher } from '@/lib/password-hasher'
import type { PasswordResetTokensRepository } from '@/repositories/password-reset-tokens-repository'
import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import { InvalidPasswordResetCodeError } from '@/use-cases/_errors/auth-errors'
import { normalizeEmail } from '@/utils/email'
import { hashResetCode } from '@/utils/secure-token'

export type ResetPasswordRequest = {
  email: string
  code: string
  newPassword: string
}

export type ResetPasswordConfig = {
  codeSecret: string
}

// with 6 digits this leaves 5 in a million to guess a code before it dies
export const MAX_RESET_CODE_ATTEMPTS = 5

const sameHash = (a: string, b: string) =>
  a.length === b.length && timingSafeEqual(Buffer.from(a), Buffer.from(b))

export class ResetPasswordUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly identities: UserIdentitiesRepository,
    private readonly resetTokens: PasswordResetTokensRepository,
    private readonly refreshTokens: RefreshTokensRepository,
    private readonly passwordHasher: PasswordHasher,
    private readonly config: ResetPasswordConfig,
  ) {}

  async execute(request: ResetPasswordRequest): Promise<void> {
    const identity = await this.identities.findByProvider('EMAIL', normalizeEmail(request.email))
    if (!identity) throw new InvalidPasswordResetCodeError()

    const user = await this.users.findById(identity.userId)
    if (user?.status !== 'ACTIVE') throw new InvalidPasswordResetCodeError()

    const token = await this.resetTokens.findActiveByUserId(user.id)
    if (!token) throw new InvalidPasswordResetCodeError()

    // count the attempt before comparing, so parallel guesses can't go over the limit
    if (!(await this.resetTokens.registerAttempt(token.id, MAX_RESET_CODE_ATTEMPTS))) {
      throw new InvalidPasswordResetCodeError()
    }

    const codeHash = hashResetCode(this.config.codeSecret, user.id, request.code)
    if (!sameHash(codeHash, token.tokenHash)) {
      if (token.attempts + 1 >= MAX_RESET_CODE_ATTEMPTS) await this.resetTokens.markAsUsed(token.id)
      throw new InvalidPasswordResetCodeError()
    }

    if (!(await this.resetTokens.markAsUsed(token.id))) {
      throw new InvalidPasswordResetCodeError()
    }

    await this.identities.update(identity.id, {
      passwordHash: await this.passwordHasher.hash(request.newPassword),
      // the code proves the user owns the inbox
      emailVerifiedAt: identity.emailVerifiedAt ?? new Date(),
    })

    await this.resetTokens.invalidateAllByUserId(user.id)
    await this.refreshTokens.revokeAllByUserId(user.id, 'PASSWORD_CHANGED')
  }
}
