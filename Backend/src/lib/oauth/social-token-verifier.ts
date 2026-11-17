import type { IdentityProvider } from '@/generated/prisma/client'

export type SocialProvider = Extract<IdentityProvider, 'GOOGLE' | 'FACEBOOK'>

export const SOCIAL_PROVIDERS: readonly SocialProvider[] = ['GOOGLE', 'FACEBOOK']

export type SocialProfile = {
  providerUserId: string
  /** only set when the provider says the email is verified */
  email: string | null
}

export type SocialTokenVerification =
  { valid: true; profile: SocialProfile } | { valid: false; reason: 'invalid' | 'unavailable' }

export interface SocialTokenVerifier {
  verify(token: string): Promise<SocialTokenVerification>
}

/** a provider without a verifier is not configured */
export type SocialTokenVerifiers = Partial<Record<SocialProvider, SocialTokenVerifier>>

export const invalidToken = { valid: false, reason: 'invalid' } as const
export const providerUnavailable = { valid: false, reason: 'unavailable' } as const
