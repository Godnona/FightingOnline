import { JoseAccessTokenService } from '@/lib/access-token'
import { InMemoryMailer } from '@/lib/mailer/in-memory-mailer'
import { MockTokenVerifier } from '@/lib/oauth/mock-token-verifier'
import type { SocialTokenVerifiers } from '@/lib/oauth/social-token-verifier'
import { AuthenticateAccessTokenUseCase } from '@/use-cases/auth/authenticate-access-token'
import { AuthenticateWithEmailUseCase } from '@/use-cases/auth/authenticate-with-email'
import { AuthenticateWithSocialUseCase } from '@/use-cases/auth/authenticate-with-social'
import { DisplayNameGenerator } from '@/use-cases/auth/display-name-generator'
import { LinkSocialIdentityUseCase } from '@/use-cases/auth/link-social-identity'
import { ListIdentitiesUseCase } from '@/use-cases/auth/list-identities'
import { LogoutUseCase } from '@/use-cases/auth/logout'
import { LogoutAllUseCase } from '@/use-cases/auth/logout-all'
import { RefreshSessionUseCase } from '@/use-cases/auth/refresh-session'
import { RegisterWithEmailUseCase } from '@/use-cases/auth/register-with-email'
import { RequestPasswordResetUseCase } from '@/use-cases/auth/request-password-reset'
import { ResetPasswordUseCase } from '@/use-cases/auth/reset-password'
import { SessionIssuer } from '@/use-cases/auth/session-issuer'
import { UnlinkIdentityUseCase } from '@/use-cases/auth/unlink-identity'

import { FakePasswordHasher } from './fakes/fake-password-hasher'
import { InMemoryDatabase } from './in-memory/in-memory-database'
import { InMemoryPasswordResetTokensRepository } from './in-memory/in-memory-password-reset-tokens-repository'
import { InMemoryRefreshTokensRepository } from './in-memory/in-memory-refresh-tokens-repository'
import { InMemoryUserIdentitiesRepository } from './in-memory/in-memory-user-identities-repository'
import { InMemoryUsersRepository } from './in-memory/in-memory-users-repository'

export const TEST_RESET_CODE_SECRET = 'unit-test-reset-code-secret'

export function makeAuthTestContext() {
  const db = new InMemoryDatabase()
  const users = new InMemoryUsersRepository(db)
  const identities = new InMemoryUserIdentitiesRepository(db)
  const refreshTokens = new InMemoryRefreshTokensRepository(db)
  const resetTokens = new InMemoryPasswordResetTokensRepository(db)
  const passwordHasher = new FakePasswordHasher()
  const mailer = new InMemoryMailer()
  const warnings: unknown[] = []
  const logger = { warn: (payload: unknown) => void warnings.push(payload) }

  const accessTokens = new JoseAccessTokenService({
    secret: 'unit-test-secret-that-is-at-least-32-chars',
    issuer: 'test-issuer',
    audience: 'test-audience',
    ttlSeconds: 900,
  })
  const sessions = new SessionIssuer(refreshTokens, accessTokens, 30)
  const displayNames = new DisplayNameGenerator(users)
  // apple left out on purpose: it plays the "not configured" provider
  const socialVerifiers: SocialTokenVerifiers = {
    GOOGLE: new MockTokenVerifier(),
    FACEBOOK: new MockTokenVerifier(),
  }

  return {
    db,
    repositories: { users, identities, refreshTokens, resetTokens },
    passwordHasher,
    mailer,
    accessTokens,
    sessions,
    socialVerifiers,
    warnings,
    socialLogin: new AuthenticateWithSocialUseCase(
      users,
      identities,
      socialVerifiers,
      displayNames,
      sessions,
    ),
    linkIdentity: new LinkSocialIdentityUseCase(identities, socialVerifiers),
    unlinkIdentity: new UnlinkIdentityUseCase(identities),
    listIdentities: new ListIdentitiesUseCase(identities),
    register: new RegisterWithEmailUseCase(
      users,
      identities,
      passwordHasher,
      displayNames,
      sessions,
    ),
    login: new AuthenticateWithEmailUseCase(users, identities, passwordHasher, sessions),
    refresh: new RefreshSessionUseCase(refreshTokens, users, sessions, logger),
    logout: new LogoutUseCase(refreshTokens),
    logoutAll: new LogoutAllUseCase(refreshTokens),
    requestPasswordReset: new RequestPasswordResetUseCase(users, identities, resetTokens, mailer, {
      ttlMinutes: 30,
      codeSecret: TEST_RESET_CODE_SECRET,
    }),
    resetPassword: new ResetPasswordUseCase(
      users,
      identities,
      resetTokens,
      refreshTokens,
      passwordHasher,
      { codeSecret: TEST_RESET_CODE_SECRET },
    ),
    authenticateAccessToken: new AuthenticateAccessTokenUseCase(accessTokens, users),
  }
}

export type AuthTestContext = ReturnType<typeof makeAuthTestContext>

export function resetCodeFromMailer(mailer: InMemoryMailer, email: string): string {
  const text = mailer.lastMessageTo(email)?.text ?? ''
  const match = /Your code: (\d{6})/.exec(text)
  if (!match?.[1]) throw new Error(`no reset email found for ${email}`)
  return match[1]
}
