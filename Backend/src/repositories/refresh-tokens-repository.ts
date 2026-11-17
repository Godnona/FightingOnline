import type { RefreshToken, TokenRevocationReason } from '@/generated/prisma/client'

export type CreateRefreshTokenData = {
  userId: string
  tokenHash: string
  familyId: string
  expiresAt: Date
  deviceInfo?: string | null
  ipAddress?: string | null
}

export interface RefreshTokensRepository {
  create(data: CreateRefreshTokenData): Promise<RefreshToken>

  findByTokenHash(tokenHash: string): Promise<RefreshToken | null>

  rotate(currentId: string, next: CreateRefreshTokenData): Promise<RefreshToken | null>

  revoke(id: string, reason: TokenRevocationReason): Promise<boolean>

  revokeFamily(familyId: string, reason: TokenRevocationReason): Promise<number>

  revokeAllByUserId(userId: string, reason: TokenRevocationReason): Promise<number>
}
