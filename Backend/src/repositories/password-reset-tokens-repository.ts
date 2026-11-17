import type { PasswordResetToken } from '@/generated/prisma/client'

export type CreatePasswordResetTokenData = {
  userId: string
  tokenHash: string
  expiresAt: Date
}

export interface PasswordResetTokensRepository {
  create(data: CreatePasswordResetTokenData): Promise<PasswordResetToken>

  /** newest token that is not used and not expired */
  findActiveByUserId(userId: string): Promise<PasswordResetToken | null>

  /** atomic: counts one attempt, false once the token is used or out of attempts */
  registerAttempt(id: string, maxAttempts: number): Promise<boolean>

  markAsUsed(id: string): Promise<boolean>

  invalidateAllByUserId(userId: string): Promise<number>

  countCreatedSince(userId: string, since: Date): Promise<number>
}
