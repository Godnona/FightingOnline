import { createRemoteJWKSet, errors, type JWTPayload, jwtVerify, type JWTVerifyGetKey } from 'jose'

import { normalizeEmail } from '@/utils/email'

import {
  invalidToken,
  providerUnavailable,
  type SocialTokenVerification,
  type SocialTokenVerifier,
} from './social-token-verifier'

export type OidcTokenVerifierOptions = {
  keys: JWTVerifyGetKey
  issuers: string[]
  audiences: string[]
  /** facebook limited login has no email_verified claim, its emails are always confirmed */
  emailIsVerified?: (payload: JWTPayload) => boolean
}

const JWKS_TIMEOUT_MS = 5000

// anything else (network, jwks endpoint down) means the provider is unavailable
const TOKEN_ERRORS = [
  errors.JWTExpired,
  errors.JWTClaimValidationFailed,
  errors.JWTInvalid,
  errors.JWSInvalid,
  errors.JWSSignatureVerificationFailed,
  errors.JWKSNoMatchingKey,
  errors.JWKSMultipleMatchingKeys,
  errors.JOSEAlgNotAllowed,
  errors.JOSENotSupported,
]

const emailVerifiedClaim = (payload: JWTPayload) =>
  payload.email_verified === true || payload.email_verified === 'true'

export function remoteKeys(url: string): JWTVerifyGetKey {
  return createRemoteJWKSet(new URL(url), { timeoutDuration: JWKS_TIMEOUT_MS })
}

export class OidcTokenVerifier implements SocialTokenVerifier {
  constructor(private readonly options: OidcTokenVerifierOptions) {}

  async verify(token: string): Promise<SocialTokenVerification> {
    let payload: JWTPayload
    try {
      ;({ payload } = await jwtVerify(token, this.options.keys, {
        issuer: this.options.issuers,
        audience: this.options.audiences,
        algorithms: ['RS256'],
        requiredClaims: ['sub', 'exp'],
      }))
    } catch (error) {
      if (TOKEN_ERRORS.some((errorClass) => error instanceof errorClass)) return invalidToken
      return providerUnavailable
    }

    if (typeof payload.sub !== 'string' || payload.sub.length === 0) return invalidToken

    const isVerified = this.options.emailIsVerified ?? emailVerifiedClaim
    const email =
      typeof payload.email === 'string' && isVerified(payload)
        ? normalizeEmail(payload.email)
        : null

    return { valid: true, profile: { providerUserId: payload.sub, email } }
  }
}
