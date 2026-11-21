import { SignJWT } from 'jose'
import { beforeEach, describe, expect, it } from 'vitest'

import { JoseAccessTokenService } from '@/lib/access-token'
import {
  AccessTokenExpiredError,
  AccountBannedError,
  InvalidAccessTokenError,
} from '@/use-cases/_errors/auth-errors'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

describe('AuthenticateAccessTokenUseCase', () => {
  let ctx: AuthTestContext
  let accessToken: string
  let userId: string

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    const { user, session } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'whatever123',
    })
    accessToken = session.accessToken
    userId = user.id
  })

  it('returns the user behind a valid token', async () => {
    const user = await ctx.authenticateAccessToken.execute({ accessToken })

    expect(user.id).toBe(userId)
  })

  it('throws AccessTokenExpiredError for expired tokens', async () => {
    const expired = await new JoseAccessTokenService({
      secret: 'unit-test-secret-that-is-at-least-32-chars',
      issuer: 'test-issuer',
      audience: 'test-audience',
      ttlSeconds: -60,
    }).sign({ sub: userId, role: 'PLAYER' })

    await expect(
      ctx.authenticateAccessToken.execute({ accessToken: expired.token }),
    ).rejects.toBeInstanceOf(AccessTokenExpiredError)
  })

  it('rejects tokens signed with another key, issuer or audience', async () => {
    const forge = (secret: string, issuer: string, audience: string) =>
      new SignJWT({ role: 'ADMIN' })
        .setProtectedHeader({ alg: 'HS256' })
        .setSubject(userId)
        .setIssuer(issuer)
        .setAudience(audience)
        .setIssuedAt()
        .setExpirationTime('5m')
        .sign(new TextEncoder().encode(secret))

    for (const token of [
      await forge('another-secret-that-is-at-least-32-chars!', 'test-issuer', 'test-audience'),
      await forge('unit-test-secret-that-is-at-least-32-chars', 'evil-issuer', 'test-audience'),
      await forge('unit-test-secret-that-is-at-least-32-chars', 'test-issuer', 'evil-audience'),
      'not-a-jwt',
    ]) {
      await expect(
        ctx.authenticateAccessToken.execute({ accessToken: token }),
      ).rejects.toBeInstanceOf(InvalidAccessTokenError)
    }
  })

  it('rejects unsigned tokens (alg "none")', async () => {
    const header = Buffer.from(JSON.stringify({ alg: 'none', typ: 'JWT' })).toString('base64url')
    const payload = Buffer.from(
      JSON.stringify({ sub: userId, role: 'ADMIN', iss: 'test-issuer', aud: 'test-audience' }),
    ).toString('base64url')

    await expect(
      ctx.authenticateAccessToken.execute({ accessToken: `${header}.${payload}.` }),
    ).rejects.toBeInstanceOf(InvalidAccessTokenError)
  })

  it('rejects banned and deleted users', async () => {
    ctx.db.users[0]!.status = 'BANNED'
    await expect(ctx.authenticateAccessToken.execute({ accessToken })).rejects.toBeInstanceOf(
      AccountBannedError,
    )

    ctx.db.users[0]!.status = 'DELETED'
    await expect(ctx.authenticateAccessToken.execute({ accessToken })).rejects.toBeInstanceOf(
      InvalidAccessTokenError,
    )
  })
})
