import type { Env } from '@/env'
import { env } from '@/env'
import { logger } from '@/lib/logger'

import { FacebookGraphVerifier, FacebookTokenVerifier } from './facebook-token-verifier'
import { MockTokenVerifier } from './mock-token-verifier'
import { OidcTokenVerifier, remoteKeys } from './oidc-token-verifier'
import type {
  SocialProvider,
  SocialTokenVerifier,
  SocialTokenVerifiers,
} from './social-token-verifier'

type SocialAuthEnv = Pick<
  Env,
  | 'GOOGLE_CLIENT_IDS'
  | 'FACEBOOK_APP_ID'
  | 'FACEBOOK_APP_SECRET'
  | 'FACEBOOK_GRAPH_API_VERSION'
  | 'SOCIAL_AUTH_MOCK'
>

export type SocialAuthMode = 'real' | 'mock' | 'disabled'

function realVerifiers(config: SocialAuthEnv): SocialTokenVerifiers {
  const verifiers: SocialTokenVerifiers = {}

  if (config.GOOGLE_CLIENT_IDS.length > 0) {
    verifiers.GOOGLE = new OidcTokenVerifier({
      keys: remoteKeys('https://www.googleapis.com/oauth2/v3/certs'),
      issuers: ['https://accounts.google.com', 'accounts.google.com'],
      audiences: config.GOOGLE_CLIENT_IDS,
    })
  }

  if (config.FACEBOOK_APP_ID && config.FACEBOOK_APP_SECRET) {
    verifiers.FACEBOOK = new FacebookTokenVerifier(
      new OidcTokenVerifier({
        keys: remoteKeys('https://limited.facebook.com/.well-known/oauth/openid/jwks/'),
        issuers: ['https://www.facebook.com', 'https://limited.facebook.com'],
        audiences: [config.FACEBOOK_APP_ID],
        emailIsVerified: () => true,
      }),
      new FacebookGraphVerifier({
        appId: config.FACEBOOK_APP_ID,
        appSecret: config.FACEBOOK_APP_SECRET,
        apiVersion: config.FACEBOOK_GRAPH_API_VERSION,
      }),
    )
  }

  return verifiers
}

/** real verifier when the provider is configured, otherwise the mock if SOCIAL_AUTH_MOCK is on */
export function buildSocialTokenVerifiers(config: SocialAuthEnv) {
  const real = realVerifiers(config)
  const verifiers: SocialTokenVerifiers = {}
  const modes = {} as Record<SocialProvider, SocialAuthMode>

  for (const provider of ['GOOGLE', 'FACEBOOK'] as const) {
    let verifier: SocialTokenVerifier | undefined = real[provider]
    modes[provider] = verifier ? 'real' : 'disabled'

    if (!verifier && config.SOCIAL_AUTH_MOCK) {
      verifier = new MockTokenVerifier()
      modes[provider] = 'mock'
    }
    if (verifier) verifiers[provider] = verifier
  }

  return { verifiers, modes }
}

const built = buildSocialTokenVerifiers(env)

if (Object.values(built.modes).includes('mock')) {
  logger.warn({ socialAuth: built.modes }, 'social login uses mock tokens for some providers')
}

export const socialTokenVerifiers: SocialTokenVerifiers = built.verifiers

export type { SocialProvider, SocialTokenVerifiers } from './social-token-verifier'
