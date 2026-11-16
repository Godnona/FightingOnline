import { randomUUID } from 'node:crypto'

import type { RefreshToken, User } from '@/generated/prisma/client'
import type { AccessTokenService } from '@/lib/access-token'
import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'
import { generateOpaqueToken, hashToken } from '@/utils/secure-token'

export type SessionMetadata = {
  deviceInfo?: string | null
  ipAddress?: string | null
}

export type AuthSession = {
  accessToken: string
  accessTokenExpiresAt: Date
  refreshToken: string
  refreshTokenExpiresAt: Date
}

const DAY_MS = 24 * 60 * 60 * 1000

export class SessionIssuer {
  constructor(
    private readonly refreshTokens: RefreshTokensRepository,
    private readonly accessTokens: AccessTokenService,
    private readonly refreshTokenTtlDays: number,
  ) {}

  async start(user: Pick<User, 'id' | 'role'>, metadata: SessionMetadata): Promise<AuthSession> {
    const refresh = this.newRefreshToken(user.id, randomUUID(), metadata)
    await this.refreshTokens.create(refresh.data)
    return this.buildSession(user, refresh.token, refresh.data.expiresAt)
  }

  async rotate(
    current: RefreshToken,
    user: Pick<User, 'id' | 'role'>,
    metadata: SessionMetadata,
  ): Promise<AuthSession | null> {
    const refresh = this.newRefreshToken(user.id, current.familyId, metadata)
    const created = await this.refreshTokens.rotate(current.id, refresh.data)
    if (!created) return null
    return this.buildSession(user, refresh.token, refresh.data.expiresAt)
  }

  private newRefreshToken(userId: string, familyId: string, metadata: SessionMetadata) {
    const token = generateOpaqueToken()
    return {
      token,
      data: {
        userId,
        familyId,
        tokenHash: hashToken(token),
        expiresAt: new Date(Date.now() + this.refreshTokenTtlDays * DAY_MS),
        deviceInfo: metadata.deviceInfo ?? null,
        ipAddress: metadata.ipAddress ?? null,
      },
    }
  }

  private async buildSession(
    user: Pick<User, 'id' | 'role'>,
    refreshToken: string,
    refreshTokenExpiresAt: Date,
  ): Promise<AuthSession> {
    const access = await this.accessTokens.sign({ sub: user.id, role: user.role })
    return {
      accessToken: access.token,
      accessTokenExpiresAt: access.expiresAt,
      refreshToken,
      refreshTokenExpiresAt,
    }
  }
}
