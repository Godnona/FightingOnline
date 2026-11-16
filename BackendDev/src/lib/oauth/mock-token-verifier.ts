import { normalizeEmail } from '@/utils/email'

import {
  invalidToken,
  type SocialTokenVerification,
  type SocialTokenVerifier,
} from './social-token-verifier'

// mock:<id> or mock:<id>:<email>
const MOCK_TOKEN = /^mock:([\w.-]{1,64})(?::([^\s:@]+@[^\s:@]+\.[^\s:@]+))?$/

/**
 * Stand-in until the real client ids exist. Ids get a "mock:" prefix so they
 * can never collide with a real provider account.
 */
export class MockTokenVerifier implements SocialTokenVerifier {
  async verify(token: string): Promise<SocialTokenVerification> {
    const match = MOCK_TOKEN.exec(token)
    if (!match?.[1]) return invalidToken

    return {
      valid: true,
      profile: {
        providerUserId: `mock:${match[1]}`,
        email: match[2] ? normalizeEmail(match[2]) : null,
      },
    }
  }
}
