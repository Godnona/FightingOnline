import { randomUUID } from 'node:crypto'

import type { PasswordResetToken } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type {
  CreatePasswordResetTokenData,
  PasswordResetTokensRepository,
} from '@/repositories/password-reset-tokens-repository'

import type { InMemoryDatabase } from './in-memory-database'

export class InMemoryPasswordResetTokensRepository implements PasswordResetTokensRepository {
  constructor(private readonly db: InMemoryDatabase) {}

  async create(data: CreatePasswordResetTokenData) {
    if (this.db.passwordResetTokens.some((token) => token.tokenHash === data.tokenHash)) {
      throw new UniqueConstraintError(['tokenHash'])
    }

    const token: PasswordResetToken = {
      id: randomUUID(),
      userId: data.userId,
      tokenHash: data.tokenHash,
      attempts: 0,
      expiresAt: data.expiresAt,
      usedAt: null,
      createdAt: new Date(),
    }
    this.db.passwordResetTokens.push(token)
    return { ...token }
  }

  async findActiveByUserId(userId: string) {
    const now = Date.now()
    const active = this.db.passwordResetTokens
      .filter(
        (token) =>
          token.userId === userId && token.usedAt === null && token.expiresAt.getTime() > now,
      )
      .sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime())
    return active[0] ? { ...active[0] } : null
  }

  async registerAttempt(id: string, maxAttempts: number) {
    const token = this.db.passwordResetTokens.find((item) => item.id === id)
    if (!token || token.usedAt !== null || token.attempts >= maxAttempts) return false
    token.attempts++
    return true
  }

  async markAsUsed(id: string) {
    const token = this.db.passwordResetTokens.find((item) => item.id === id)
    if (!token || token.usedAt !== null) return false
    token.usedAt = new Date()
    return true
  }

  async invalidateAllByUserId(userId: string) {
    const now = new Date()
    let count = 0
    for (const token of this.db.passwordResetTokens) {
      if (token.userId === userId && token.usedAt === null) {
        token.usedAt = now
        count++
      }
    }
    return count
  }

  async countCreatedSince(userId: string, since: Date) {
    return this.db.passwordResetTokens.filter(
      (token) => token.userId === userId && token.createdAt.getTime() >= since.getTime(),
    ).length
  }
}
