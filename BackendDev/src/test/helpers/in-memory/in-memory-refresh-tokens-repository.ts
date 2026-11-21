import { randomUUID } from 'node:crypto'

import type { RefreshToken, TokenRevocationReason } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type {
  CreateRefreshTokenData,
  RefreshTokensRepository,
} from '@/repositories/refresh-tokens-repository'

import type { InMemoryDatabase } from './in-memory-database'

export class InMemoryRefreshTokensRepository implements RefreshTokensRepository {
  constructor(private readonly db: InMemoryDatabase) {}

  private insert(data: CreateRefreshTokenData): RefreshToken {
    if (this.db.refreshTokens.some((token) => token.tokenHash === data.tokenHash)) {
      throw new UniqueConstraintError(['tokenHash'])
    }

    const token: RefreshToken = {
      id: randomUUID(),
      userId: data.userId,
      tokenHash: data.tokenHash,
      familyId: data.familyId,
      expiresAt: data.expiresAt,
      revokedAt: null,
      revokedReason: null,
      replacedById: null,
      deviceInfo: data.deviceInfo ?? null,
      ipAddress: data.ipAddress ?? null,
      createdAt: new Date(),
    }
    this.db.refreshTokens.push(token)
    return token
  }

  private revokeWhere(
    predicate: (token: RefreshToken) => boolean,
    reason: TokenRevocationReason,
  ): number {
    const now = new Date()
    let count = 0
    for (const token of this.db.refreshTokens) {
      if (token.revokedAt === null && predicate(token)) {
        token.revokedAt = now
        token.revokedReason = reason
        count++
      }
    }
    return count
  }

  async create(data: CreateRefreshTokenData) {
    return { ...this.insert(data) }
  }

  async findByTokenHash(tokenHash: string) {
    const token = this.db.refreshTokens.find((item) => item.tokenHash === tokenHash)
    return token ? { ...token } : null
  }

  async rotate(currentId: string, next: CreateRefreshTokenData) {
    const current = this.db.refreshTokens.find((item) => item.id === currentId)
    if (!current || current.revokedAt !== null) return null

    const created = this.insert(next)
    current.revokedAt = new Date()
    current.revokedReason = 'ROTATED'
    current.replacedById = created.id
    return { ...created }
  }

  async revoke(id: string, reason: TokenRevocationReason) {
    return this.revokeWhere((token) => token.id === id, reason) > 0
  }

  async revokeFamily(familyId: string, reason: TokenRevocationReason) {
    return this.revokeWhere((token) => token.familyId === familyId, reason)
  }

  async revokeAllByUserId(userId: string, reason: TokenRevocationReason) {
    return this.revokeWhere((token) => token.userId === userId, reason)
  }
}
