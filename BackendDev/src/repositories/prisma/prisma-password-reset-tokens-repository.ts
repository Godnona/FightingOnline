import type { PrismaClient } from '@/generated/prisma/client'
import type {
  CreatePasswordResetTokenData,
  PasswordResetTokensRepository,
} from '@/repositories/password-reset-tokens-repository'

export class PrismaPasswordResetTokensRepository implements PasswordResetTokensRepository {
  constructor(private readonly db: PrismaClient) {}

  create(data: CreatePasswordResetTokenData) {
    return this.db.passwordResetToken.create({ data })
  }

  findActiveByUserId(userId: string) {
    return this.db.passwordResetToken.findFirst({
      where: { userId, usedAt: null, expiresAt: { gt: new Date() } },
      orderBy: { createdAt: 'desc' },
    })
  }

  async registerAttempt(id: string, maxAttempts: number) {
    const { count } = await this.db.passwordResetToken.updateMany({
      where: { id, usedAt: null, attempts: { lt: maxAttempts } },
      data: { attempts: { increment: 1 } },
    })
    return count > 0
  }

  async markAsUsed(id: string) {
    const { count } = await this.db.passwordResetToken.updateMany({
      where: { id, usedAt: null },
      data: { usedAt: new Date() },
    })
    return count > 0
  }

  async invalidateAllByUserId(userId: string) {
    const { count } = await this.db.passwordResetToken.updateMany({
      where: { userId, usedAt: null },
      data: { usedAt: new Date() },
    })
    return count
  }

  countCreatedSince(userId: string, since: Date) {
    return this.db.passwordResetToken.count({ where: { userId, createdAt: { gte: since } } })
  }
}
