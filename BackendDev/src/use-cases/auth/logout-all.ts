import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'

export type LogoutAllRequest = {
  userId: string
}

export type LogoutAllResponse = {
  revokedSessions: number
}

export class LogoutAllUseCase {
  constructor(private readonly refreshTokens: RefreshTokensRepository) {}

  async execute({ userId }: LogoutAllRequest): Promise<LogoutAllResponse> {
    const revokedSessions = await this.refreshTokens.revokeAllByUserId(userId, 'LOGOUT_ALL')
    return { revokedSessions }
  }
}
