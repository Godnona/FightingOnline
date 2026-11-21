import type { PrismaClient, TokenRevocationReason } from '@/generated/prisma/client'
import type {
  CreateRefreshTokenData,
  RefreshTokensRepository,
} from '@/repositories/refresh-tokens-repository'

export class PrismaRefreshTokensRepository implements RefreshTokensRepository {
  constructor(private readonly db: PrismaClient) {}

  create(data: CreateRefreshTokenData) {
    return this.db.refreshToken.create({ data })
  }

  findByTokenHash(tokenHash: string) {
    return this.db.refreshToken.findUnique({ where: { tokenHash } })
  }

  rotate(currentId: string, next: CreateRefreshTokenData) {
    return this.db.$transaction(async (tx) => {
      // row lock: if two requests rotate the same token, only one gets count = 1
      const { count } = await tx.refreshToken.updateMany({
        where: { id: currentId, revokedAt: null },
        data: { revokedAt: new Date(), revokedReason: 'ROTATED' },
      })

      if (count === 0) return null

      const created = await tx.refreshToken.create({ data: next })

      await tx.refreshToken.update({
        where: { id: currentId },
        data: { replacedById: created.id },
      })

      return created
    })
  }

  async revoke(id: string, reason: TokenRevocationReason) {
    const { count } = await this.db.refreshToken.updateMany({
      where: { id, revokedAt: null },
      data: { revokedAt: new Date(), revokedReason: reason },
    })
    return count > 0
  }

  async revokeFamily(familyId: string, reason: TokenRevocationReason) {
    const { count } = await this.db.refreshToken.updateMany({
      where: { familyId, revokedAt: null },
      data: { revokedAt: new Date(), revokedReason: reason },
    })
    return count
  }

  async revokeAllByUserId(userId: string, reason: TokenRevocationReason) {
    const { count } = await this.db.refreshToken.updateMany({
      where: { userId, revokedAt: null },
      data: { revokedAt: new Date(), revokedReason: reason },
    })
    return count
  }
}
