import { errors, jwtVerify, SignJWT } from 'jose'

import type { Role } from '@/generated/prisma/client'

export type AccessTokenClaims = {
  sub: string
  role: Role
}

export type SignedAccessToken = {
  token: string
  expiresAt: Date
}

export type AccessTokenVerification =
  { valid: true; claims: AccessTokenClaims } | { valid: false; reason: 'expired' | 'invalid' }

export interface AccessTokenService {
  sign(claims: AccessTokenClaims): Promise<SignedAccessToken>
  verify(token: string): Promise<AccessTokenVerification>
}

export type JoseAccessTokenServiceOptions = {
  secret: string
  issuer: string
  audience: string
  ttlSeconds: number
}

const ALGORITHM = 'HS256'
const ROLES: readonly string[] = ['PLAYER', 'ADMIN']

export class JoseAccessTokenService implements AccessTokenService {
  private readonly key: Uint8Array

  constructor(private readonly options: JoseAccessTokenServiceOptions) {
    this.key = new TextEncoder().encode(options.secret)
  }

  async sign({ sub, role }: AccessTokenClaims): Promise<SignedAccessToken> {
    const issuedAt = Math.floor(Date.now() / 1000)
    const expiresAtSeconds = issuedAt + this.options.ttlSeconds

    const token = await new SignJWT({ role })
      .setProtectedHeader({ alg: ALGORITHM, typ: 'JWT' })
      .setSubject(sub)
      .setIssuer(this.options.issuer)
      .setAudience(this.options.audience)
      .setIssuedAt(issuedAt)
      .setExpirationTime(expiresAtSeconds)
      .sign(this.key)

    return { token, expiresAt: new Date(expiresAtSeconds * 1000) }
  }

  async verify(token: string): Promise<AccessTokenVerification> {
    try {
      const { payload } = await jwtVerify(token, this.key, {
        algorithms: [ALGORITHM],
        issuer: this.options.issuer,
        audience: this.options.audience,
        requiredClaims: ['sub', 'exp', 'iat'],
      })

      if (typeof payload.sub !== 'string' || !ROLES.includes(payload.role as string)) {
        return { valid: false, reason: 'invalid' }
      }

      return { valid: true, claims: { sub: payload.sub, role: payload.role as Role } }
    } catch (error) {
      if (error instanceof errors.JWTExpired) return { valid: false, reason: 'expired' }
      return { valid: false, reason: 'invalid' }
    }
  }
}
