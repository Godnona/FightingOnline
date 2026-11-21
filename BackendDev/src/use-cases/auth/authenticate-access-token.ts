import type { User } from '@/generated/prisma/client'
import type { AccessTokenService } from '@/lib/access-token'
import type { UsersRepository } from '@/repositories/users-repository'
import {
  AccessTokenExpiredError,
  AccountBannedError,
  InvalidAccessTokenError,
} from '@/use-cases/_errors/auth-errors'

export type AuthenticateAccessTokenRequest = {
  accessToken: string
}

export class AuthenticateAccessTokenUseCase {
  constructor(
    private readonly accessTokens: AccessTokenService,
    private readonly users: UsersRepository,
  ) {}

  async execute({ accessToken }: AuthenticateAccessTokenRequest): Promise<User> {
    const result = await this.accessTokens.verify(accessToken)

    if (!result.valid) {
      throw result.reason === 'expired'
        ? new AccessTokenExpiredError()
        : new InvalidAccessTokenError()
    }

    const user = await this.users.findById(result.claims.sub)
    if (!user || user.status === 'DELETED') throw new InvalidAccessTokenError()
    if (user.status === 'BANNED') throw new AccountBannedError()

    return user
  }
}
