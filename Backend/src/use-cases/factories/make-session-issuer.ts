import { env } from '@/env'
import { accessTokenService } from '@/lib/auth-services'
import { SessionIssuer } from '@/use-cases/auth/session-issuer'

import { makeRefreshTokensRepository } from './make-repositories'

export function makeSessionIssuer() {
  return new SessionIssuer(
    makeRefreshTokensRepository(),
    accessTokenService,
    env.REFRESH_TOKEN_TTL_DAYS,
  )
}
