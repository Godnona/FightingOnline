import type { Logger } from 'pino'

import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import { AccountBannedError, InvalidRefreshTokenError } from '@/use-cases/_errors/auth-errors'
import { hashToken } from '@/utils/secure-token'

import type { AuthSession, SessionIssuer, SessionMetadata } from './session-issuer'

export type RefreshSessionRequest = SessionMetadata & {
  refreshToken: string
}

export type RefreshSessionResponse = {
  session: AuthSession
}

export class RefreshSessionUseCase {
  constructor(
    private readonly refreshTokens: RefreshTokensRepository,
    private readonly users: UsersRepository,
    private readonly sessions: SessionIssuer,
    private readonly logger: Pick<Logger, 'warn'>,
  ) {}

  async execute(request: RefreshSessionRequest): Promise<RefreshSessionResponse> {
    const token = await this.refreshTokens.findByTokenHash(hashToken(request.refreshToken))
    if (!token) throw new InvalidRefreshTokenError()

    if (token.revokedAt) {
      if (token.revokedReason === 'ROTATED') {
        await this.reuseDetected(token.familyId, token.userId)
      }
      throw new InvalidRefreshTokenError()
    }

    if (token.expiresAt.getTime() <= Date.now()) throw new InvalidRefreshTokenError()

    const user = await this.users.findById(token.userId)
    if (!user || user.status === 'DELETED') {
      await this.refreshTokens.revokeFamily(token.familyId, 'ADMIN')
      throw new InvalidRefreshTokenError()
    }
    if (user.status === 'BANNED') {
      await this.refreshTokens.revokeFamily(token.familyId, 'ADMIN')
      throw new AccountBannedError()
    }

    const session = await this.sessions.rotate(token, user, request)
    if (!session) {
      await this.reuseDetected(token.familyId, token.userId)
      throw new InvalidRefreshTokenError()
    }

    return { session }
  }

  private async reuseDetected(familyId: string, userId: string) {
    const revoked = await this.refreshTokens.revokeFamily(familyId, 'REUSE_DETECTED')
    this.logger.warn({ userId, familyId, revoked }, 'refresh token reuse detected')
  }
}
