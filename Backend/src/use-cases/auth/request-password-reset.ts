import type { Mailer } from '@/lib/mailer/mailer'
import { passwordResetEmail } from '@/lib/mailer/templates/password-reset-email'
import type { PasswordResetTokensRepository } from '@/repositories/password-reset-tokens-repository'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import { normalizeEmail } from '@/utils/email'
import { generateResetCode, hashResetCode } from '@/utils/secure-token'

export type RequestPasswordResetRequest = {
  email: string
}

export type PasswordResetConfig = {
  ttlMinutes: number
  codeSecret: string
}

// each code costs an email, so cap them per account on top of the per-ip rate limit
export const MAX_RESET_CODES_PER_HOUR = 5

const HOUR_MS = 60 * 60 * 1000

export class RequestPasswordResetUseCase {
  constructor(
    private readonly users: UsersRepository,
    private readonly identities: UserIdentitiesRepository,
    private readonly resetTokens: PasswordResetTokensRepository,
    private readonly mailer: Mailer,
    private readonly config: PasswordResetConfig,
  ) {}

  async execute(request: RequestPasswordResetRequest): Promise<void> {
    const identity = await this.identities.findByProvider('EMAIL', normalizeEmail(request.email))
    if (!identity?.email) return

    const user = await this.users.findById(identity.userId)
    if (user?.status !== 'ACTIVE') return

    const recent = await this.resetTokens.countCreatedSince(user.id, new Date(Date.now() - HOUR_MS))
    if (recent >= MAX_RESET_CODES_PER_HOUR) return

    await this.resetTokens.invalidateAllByUserId(user.id)

    const code = generateResetCode()
    await this.resetTokens.create({
      userId: user.id,
      tokenHash: hashResetCode(this.config.codeSecret, user.id, code),
      expiresAt: new Date(Date.now() + this.config.ttlMinutes * 60 * 1000),
    })

    await this.mailer.send(
      passwordResetEmail({ to: identity.email, code, expiresInMinutes: this.config.ttlMinutes }),
    )
  }
}
