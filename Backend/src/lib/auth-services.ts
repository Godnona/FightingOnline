import { env } from '@/env'

import { JoseAccessTokenService } from './access-token'
import { Argon2PasswordHasher } from './password-hasher'

export const passwordHasher = new Argon2PasswordHasher()

export const accessTokenService = new JoseAccessTokenService({
  secret: env.JWT_SECRET,
  issuer: env.JWT_ISSUER,
  audience: env.JWT_AUDIENCE,
  ttlSeconds: env.ACCESS_TOKEN_TTL_SECONDS,
})
