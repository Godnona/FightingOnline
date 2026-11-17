import { afterAll, beforeAll, beforeEach, describe, expect, it } from 'vitest'

import { buildApp } from '@/app'
import { prisma } from '@/lib/prisma'
import { resetDatabase } from '@/test/helpers/reset-database'

type SocialAuthBody = {
  user: { id: string; displayName: string }
  session: { accessToken: string; refreshToken: string }
  isNewUser: boolean
}

type IdentityBody = {
  provider: string
  email: string | null
  linkedAt: string
  lastUsedAt: string | null
}

describe('social auth routes', () => {
  const app = buildApp()

  let ipCounter = 0
  let ip = ''

  const request = (
    method: 'GET' | 'POST' | 'DELETE',
    url: string,
    options: { payload?: object; accessToken?: string } = {},
  ) =>
    app.inject({
      method,
      url,
      payload: options.payload,
      headers: options.accessToken ? { authorization: `Bearer ${options.accessToken}` } : {},
      remoteAddress: ip,
    })

  const socialLogin = (provider: string, token: string) =>
    request('POST', `/auth/${provider}`, { payload: { token } })

  const registerWithEmail = async (email = 'player@example.com') => {
    const response = await request('POST', '/auth/register', {
      payload: { email, password: 'a-good-password' },
    })
    expect(response.statusCode).toBe(201)
    return response.json<SocialAuthBody>()
  }

  beforeAll(async () => {
    await app.ready()
  })

  beforeEach(async () => {
    ipCounter++
    ip = `10.1.${Math.floor(ipCounter / 250)}.${(ipCounter % 250) + 1}`
    await resetDatabase(prisma)
  })

  afterAll(async () => {
    await app.close()
  })

  describe('POST /auth/google, /auth/facebook, /auth/apple', () => {
    it.each(['google', 'facebook', 'apple'])('creates the account with %s', async (provider) => {
      const response = await socialLogin(provider, 'mock:p1:player@example.com')

      expect(response.statusCode).toBe(200)
      const body = response.json<SocialAuthBody>()
      expect(body.isNewUser).toBe(true)
      expect(body.user.displayName).toMatch(/^Player\d{6}$/)
      expect(body.session.refreshToken).toBeTruthy()

      const identity = await prisma.userIdentity.findFirstOrThrow()
      expect(identity).toMatchObject({
        provider: provider.toUpperCase(),
        providerUserId: 'mock:p1',
        email: 'player@example.com',
      })
    })

    it('signs in to the existing account', async () => {
      const first = (await socialLogin('google', 'mock:p1')).json<SocialAuthBody>()
      const second = await socialLogin('google', 'mock:p1')

      expect(second.statusCode).toBe(200)
      expect(second.json<SocialAuthBody>()).toMatchObject({
        isNewUser: false,
        user: { id: first.user.id },
      })
    })

    it('returns 409 when the email belongs to another account', async () => {
      await registerWithEmail('taken@example.com')

      const response = await socialLogin('google', 'mock:p1:taken@example.com')

      expect(response.statusCode).toBe(409)
      expect(response.json()).toMatchObject({ code: 'EMAIL_ALREADY_IN_USE' })
    })

    it('returns 401 for an invalid provider token', async () => {
      const response = await socialLogin('facebook', 'definitely-not-valid')

      expect(response.statusCode).toBe(401)
      expect(response.json()).toMatchObject({ code: 'INVALID_PROVIDER_TOKEN' })
    })

    it('validates the body', async () => {
      const response = await request('POST', '/auth/google', { payload: { token: '' } })

      expect(response.statusCode).toBe(400)
      expect(response.json()).toMatchObject({ code: 'VALIDATION_ERROR' })
    })
  })

  describe('/auth/identities', () => {
    it('lists, links and unlinks identities', async () => {
      const { session } = await registerWithEmail()
      const accessToken = session.accessToken

      const linked = await request('POST', '/auth/identities/google', {
        payload: { token: 'mock:g1:player@example.com' },
        accessToken,
      })
      expect(linked.statusCode).toBe(200)
      expect(linked.json<{ identity: IdentityBody }>().identity).toMatchObject({
        provider: 'GOOGLE',
        email: 'player@example.com',
        lastUsedAt: null,
      })

      const listed = await request('GET', '/auth/identities', { accessToken })
      expect(listed.statusCode).toBe(200)
      const providers = listed
        .json<{ identities: IdentityBody[] }>()
        .identities.map((identity) => identity.provider)
      expect(providers).toEqual(['EMAIL', 'GOOGLE'])

      expect((await request('DELETE', '/auth/identities/email', { accessToken })).statusCode).toBe(
        204,
      )

      const last = await request('DELETE', '/auth/identities/google', { accessToken })
      expect(last.statusCode).toBe(409)
      expect(last.json()).toMatchObject({ code: 'LAST_IDENTITY' })
    })

    it('returns 409 when the provider account belongs to someone else', async () => {
      await socialLogin('google', 'mock:g1')
      const { session } = await registerWithEmail()

      const response = await request('POST', '/auth/identities/google', {
        payload: { token: 'mock:g1' },
        accessToken: session.accessToken,
      })

      expect(response.statusCode).toBe(409)
      expect(response.json()).toMatchObject({ code: 'IDENTITY_ALREADY_LINKED' })
    })

    it('returns 404 when unlinking a provider the player does not have', async () => {
      const { session } = await registerWithEmail()

      const response = await request('DELETE', '/auth/identities/apple', {
        accessToken: session.accessToken,
      })

      expect(response.statusCode).toBe(404)
      expect(response.json()).toMatchObject({ code: 'IDENTITY_NOT_FOUND' })
    })

    it('requires authentication and a known provider', async () => {
      expect((await request('GET', '/auth/identities')).statusCode).toBe(401)

      const { session } = await registerWithEmail()
      const response = await request('POST', '/auth/identities/steam', {
        payload: { token: 'mock:s1' },
        accessToken: session.accessToken,
      })
      expect(response.statusCode).toBe(400)
    })
  })
})
