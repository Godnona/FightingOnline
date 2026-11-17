import type {
  SocialProfile,
  SocialProvider,
  SocialTokenVerifiers,
} from '@/lib/oauth/social-token-verifier'
import {
  InvalidProviderTokenError,
  ProviderNotConfiguredError,
  ProviderUnavailableError,
} from '@/use-cases/_errors/auth-errors'

const providerNames: Record<SocialProvider, string> = {
  GOOGLE: 'Google',
  FACEBOOK: 'Facebook',
  APPLE: 'Apple',
}

export async function verifySocialToken(
  verifiers: SocialTokenVerifiers,
  provider: SocialProvider,
  token: string,
): Promise<SocialProfile> {
  const verifier = verifiers[provider]
  if (!verifier) throw new ProviderNotConfiguredError(providerNames[provider])

  const result = await verifier.verify(token)
  if (result.valid) return result.profile

  throw result.reason === 'unavailable'
    ? new ProviderUnavailableError(providerNames[provider])
    : new InvalidProviderTokenError()
}
