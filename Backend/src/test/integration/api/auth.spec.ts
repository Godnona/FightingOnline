import { afterAll, beforeAll, beforeEach, describe, expect, it } from 'vitest'

import { buildApp } from '@/app'
import { mailer } from '@/lib/mailer'
import type { InMemoryMailer } from '@/lib/mailer/in-memory-mailer'
import { prisma } from '@/lib/prisma'
import { resetCodeFromMailer } from '@/test/helpers/make-auth-test-context'
import { resetDatabase } from '@/test/helpers/reset-database'

type AuthBody = {
  user: { id: string; displayName: string; avatar: string | null; role: string }
  session: {
    tokenType: string
    accessToken: string
    accessTokenExpiresAt: string
    refreshToken: string
    refreshTokenExpiresAt: string
  }
}

describe('auth routes', () => {
  const app = buildApp()
  const outbox = mailer as InMemoryMailer

  let ipCounter = 0
  let ip = ''

  const post = (url: string, payload?: object, headers?: Record<string, string>) =>
    app.inject({ method: 'POST', url, payload, headers, remoteAddress: ip })

  const register = async (email = 'player@example.com', extra: object = {}) => {
    const response = await post('/auth/register', { email, password: 'a-good-password', ...extra })
    expect(response.statusCode).toBe(201)
    return response.json<AuthBody>()
  }

  beforeAll(async () => {
    await app.ready()
  })

  beforeEach(async () => {
    // new ip per test so the rate limit counter doesn't carry over
    ipCounter++
    ip = `10.0.${Math.floor(ipCounter / 250)}.${(ipCounter % 250) + 1}`
    outbox.clear()
    await resetDatabase(prisma)
  })

  afterAll(async () => {
    await app.close()
  })

  describe('POST /auth/register', () => {
    it('creates the account', async () => {
      const body = await register('New@Example.com', { displayName: 'NewPlayer' })

      expect(body.user).toEqual({
        id: expect.any(String) as string,
        displayName: 'NewPlayer',
        avatar: null,
        role: 'PLAYER',
      })
      expect(body.session).toMatchObject({ tokenType: 'Bearer' })

      const identity = await prisma.userIdentity.findFirstOrThrow()
      expect(identity.email).toBe('new@example.com')
      expect(identity.passwordHash).toMatch(/^\$argon2id\$/)
      const token = await prisma.refreshToken.findFirstOrThrow()
      expect(token.tokenHash).not.toBe(body.session.refreshToken)
    })

    it('generates a display name when omitted', async () => {
      const { user } = await register()

      expect(user.displayName).toMatch(/^Player\d{6}$/)
    })

    it('validates every field', async () => {
      const response = await post('/auth/register', {
        email: 'not-an-email',
        password: 'password123',
        displayName: 'bad name!',
      })

      expect(response.statusCode).toBe(400)
      const paths = response
        .json<{ details: { issues: { path: string }[] } }>()
        .details.issues.map((issue) => issue.path)
      expect(paths).toEqual(expect.arrayContaining(['/email', '/password', '/displayName']))
    })

    it('rejects duplicated emails and display names with 409', async () => {
      await register('taken@example.com', { displayName: 'Taken' })

      const sameEmail = await post('/auth/register', {
        email: 'TAKEN@example.com',
        password: 'a-good-password',
      })
      const sameName = await post('/auth/register', {
        email: 'other@example.com',
        password: 'a-good-password',
        displayName: 'TAKEN',
      })

      expect(sameEmail.statusCode).toBe(409)
      expect(sameEmail.json()).toMatchObject({ code: 'EMAIL_ALREADY_IN_USE' })
      expect(sameName.statusCode).toBe(409)
      expect(sameName.json()).toMatchObject({ code: 'DISPLAY_NAME_TAKEN' })
    })
  })

  describe('POST /auth/login', () => {
    it('signs in with the right password', async () => {
      const { user } = await register()

      const response = await post('/auth/login', {
        email: 'player@example.com',
        password: 'a-good-password',
      })

      expect(response.statusCode).toBe(200)
      expect(response.json<AuthBody>().user.id).toBe(user.id)
    })

    it('same response for wrong password and unknown email', async () => {
      await register()

      const wrongPassword = await post('/auth/login', {
        email: 'player@example.com',
        password: 'wrong-password',
      })
      const unknownEmail = await post('/auth/login', {
        email: 'ghost@example.com',
        password: 'wrong-password',
      })

      expect(wrongPassword.statusCode).toBe(401)
      expect(unknownEmail.statusCode).toBe(401)
      expect(wrongPassword.body).toBe(unknownEmail.body)
      expect(wrongPassword.json()).toMatchObject({ code: 'INVALID_CREDENTIALS' })
    })

    it('is rate limited per IP', async () => {
      const attempts = []
      for (let i = 0; i < 11; i++) {
        attempts.push(await post('/auth/login', { email: 'x@example.com', password: 'nope' }))
      }

      expect(attempts.slice(0, 10).every((r) => r.statusCode === 401)).toBe(true)
      const limited = attempts[10]!
      expect(limited.statusCode).toBe(429)
      expect(limited.json()).toMatchObject({ code: 'RATE_LIMITED' })
      expect(limited.headers['retry-after']).toBeDefined()
    })
  })

  describe('POST /auth/refresh', () => {
    it('rotates tokens and detects reuse of an old one', async () => {
      const { session } = await register()

      const first = await post('/auth/refresh', { refreshToken: session.refreshToken })
      expect(first.statusCode).toBe(200)
      const rotated = first.json<{ session: AuthBody['session'] }>().session
      expect(rotated.refreshToken).not.toBe(session.refreshToken)

      const reuse = await post('/auth/refresh', { refreshToken: session.refreshToken })
      expect(reuse.statusCode).toBe(401)
      expect(reuse.json()).toMatchObject({ code: 'INVALID_REFRESH_TOKEN' })

      const afterReuse = await post('/auth/refresh', { refreshToken: rotated.refreshToken })
      expect(afterReuse.statusCode).toBe(401)
    })
  })

  describe('logout', () => {
    it('logout returns 204', async () => {
      const { session } = await register()

      const response = await post('/auth/logout', { refreshToken: session.refreshToken })
      const unknown = await post('/auth/logout', { refreshToken: 'unknown-token' })

      expect(response.statusCode).toBe(204)
      expect(response.body).toBe('')
      expect(unknown.statusCode).toBe(204)
      const refresh = await post('/auth/refresh', { refreshToken: session.refreshToken })
      expect(refresh.statusCode).toBe(401)
    })

    it('logout-all requires a token', async () => {
      const missing = await post('/auth/logout-all')
      const invalid = await post('/auth/logout-all', undefined, {
        authorization: 'Bearer not-a-jwt',
      })

      expect(missing.statusCode).toBe(401)
      expect(missing.json()).toMatchObject({ code: 'AUTHENTICATION_REQUIRED' })
      expect(invalid.statusCode).toBe(401)
      expect(invalid.json()).toMatchObject({ code: 'INVALID_ACCESS_TOKEN' })
    })

    it('logout-all ends every session', async () => {
      const { session: phone } = await register()
      const pc = (
        await post('/auth/login', { email: 'player@example.com', password: 'a-good-password' })
      ).json<AuthBody>().session

      const response = await post('/auth/logout-all', undefined, {
        authorization: `Bearer ${pc.accessToken}`,
      })

      expect(response.statusCode).toBe(204)
      for (const refreshToken of [phone.refreshToken, pc.refreshToken]) {
        expect((await post('/auth/refresh', { refreshToken })).statusCode).toBe(401)
      }
    })

    it('blocks banned accounts on protected routes', async () => {
      const { user, session } = await register()
      await prisma.user.update({ where: { id: user.id }, data: { status: 'BANNED' } })

      const response = await post('/auth/logout-all', undefined, {
        authorization: `Bearer ${session.accessToken}`,
      })

      expect(response.statusCode).toBe(403)
      expect(response.json()).toMatchObject({ code: 'ACCOUNT_BANNED' })
    })
  })

  describe('password reset', () => {
    it('same response for known and unknown emails', async () => {
      await register()

      const known = await post('/auth/password/forgot', { email: 'player@example.com' })
      const unknown = await post('/auth/password/forgot', { email: 'ghost@example.com' })

      expect(known.statusCode).toBe(202)
      expect(unknown.statusCode).toBe(202)
      expect(known.body).toBe(unknown.body)
      expect(outbox.outbox).toHaveLength(1)
    })

    it('resets the password with the emailed code', async () => {
      const { session } = await register()
      await post('/auth/password/forgot', { email: 'player@example.com' })
      const code = resetCodeFromMailer(outbox, 'player@example.com')
      const typed = `${code.slice(0, 3)} ${code.slice(3)}`

      const reset = await post('/auth/password/reset', {
        email: 'Player@Example.com',
        code: typed,
        newPassword: 'brand-new-secret',
      })
      expect(reset.statusCode).toBe(204)
      const stored = await prisma.passwordResetToken.findFirstOrThrow()
      expect(stored).toMatchObject({ attempts: 1, usedAt: expect.any(Date) as Date })

      const again = await post('/auth/password/reset', {
        email: 'player@example.com',
        code,
        newPassword: 'another-secret',
      })
      expect(again.statusCode).toBe(400)
      expect(again.json()).toMatchObject({ code: 'INVALID_PASSWORD_RESET_CODE' })

      const oldSession = await post('/auth/refresh', { refreshToken: session.refreshToken })
      expect(oldSession.statusCode).toBe(401)

      const oldPassword = await post('/auth/login', {
        email: 'player@example.com',
        password: 'a-good-password',
      })
      const newPassword = await post('/auth/login', {
        email: 'player@example.com',
        password: 'brand-new-secret',
      })
      expect(oldPassword.statusCode).toBe(401)
      expect(newPassword.statusCode).toBe(200)
    })

    it('validates the code and the new password', async () => {
      const response = await post('/auth/password/reset', {
        email: 'player@example.com',
        code: '12ab',
        newPassword: 'short',
      })

      expect(response.statusCode).toBe(400)
      expect(response.json()).toMatchObject({ code: 'VALIDATION_ERROR' })
      const paths = response
        .json<{ details: { issues: { path: string }[] } }>()
        .details.issues.map((issue) => issue.path)
      expect(paths).toEqual(expect.arrayContaining(['/code', '/newPassword']))
    })
  })
})
