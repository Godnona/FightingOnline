import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'
import { hashToken } from '@/utils/secure-token'

export type LogoutRequest = {
  refreshToken: string
}

export class LogoutUseCase {
  constructor(private readonly refreshTokens: RefreshTokensRepository) {}

  async execute({ refreshToken }: LogoutRequest): Promise<void> {
    const token = await this.refreshTokens.findByTokenHash(hashToken(refreshToken))
    if (token) await this.refreshTokens.revokeFamily(token.familyId, 'LOGOUT')
  }
}
