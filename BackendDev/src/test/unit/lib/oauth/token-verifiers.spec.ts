import { createLocalJWKSet, exportJWK, generateKeyPair, type JWTPayload, SignJWT } from 'jose'
import { beforeAll, describe, expect, it } from 'vitest'

import { buildSocialTokenVerifiers } from '@/lib/oauth'
import { FacebookGraphVerifier, FacebookTokenVerifier } from '@/lib/oauth/facebook-token-verifier'
import { MockTokenVerifier } from '@/lib/oauth/mock-token-verifier'
import { OidcTokenVerifier } from '@/lib/oauth/oidc-token-verifier'
import type { SocialTokenVerifier } from '@/lib/oauth/social-token-verifier'

const ISSUER = 'https://accounts.example.com'
const AUDIENCE = 'client-id-1'

type PrivateKey = Awaited<ReturnType<typeof generateKeyPair>>['privateKey']

describe('OidcTokenVerifier', () => {
  let privateKey: PrivateKey
  let otherPrivateKey: PrivateKey
  let verifier: OidcTokenVerifier

  beforeAll(async () => {
    const pair = await generateKeyPair('RS256', { extractable: true })
    const other = await generateKeyPair('RS256')
    privateKey = pair.privateKey
    otherPrivateKey = other.privateKey

    const jwk = { ...(await exportJWK(pair.publicKey)), kid: 'key-1', alg: 'RS256' }
    verifier = new OidcTokenVerifier({
      keys: createLocalJWKSet({ keys: [jwk] }),
      issuers: [ISSUER],
      audiences: ['other-client', AUDIENCE],
    })
  })

  type SignOptions = { key?: PrivateKey; exp?: number | string; aud?: string; iss?: string }

  const sign = (claims: JWTPayload = {}, options: SignOptions = {}) =>
    new SignJWT({ email: 'Player@Example.com', email_verified: true, ...claims })
      .setProtectedHeader({ alg: 'RS256', kid: 'key-1' })
      .setSubject('provider-user-1')
      .setIssuer(options.iss ?? ISSUER)
      .setAudience(options.aud ?? AUDIENCE)
      .setIssuedAt()
      .setExpirationTime(options.exp ?? '10m')
      .sign(options.key ?? privateKey)

  it('accepts a valid token from any configured audience', async () => {
    expect(await verifier.verify(await sign())).toEqual({
      valid: true,
      profile: { providerUserId: 'provider-user-1', email: 'player@example.com' },
    })
  })

  it('drops unverified emails', async () => {
    const result = await verifier.verify(await sign({ email_verified: false }))
    expect(result).toMatchObject({ valid: true, profile: { email: null } })

    // apple sends the flag as a string
    const apple = await verifier.verify(await sign({ email_verified: 'true' }))
    expect(apple).toMatchObject({ valid: true, profile: { email: 'player@example.com' } })
  })

  it.each([
    ['wrong audience', () => sign({}, { aud: 'someone-else' })],
    ['wrong issuer', () => sign({}, { iss: 'https://evil.example.com' })],
    ['expired', () => sign({}, { exp: Math.floor(Date.now() / 1000) - 60 })],
    ['wrong signature', () => sign({}, { key: otherPrivateKey })],
    ['garbage', async () => 'not.a.jwt'],
  ])('rejects tokens: %s', async (_case, makeToken) => {
    expect(await verifier.verify(await makeToken())).toEqual({ valid: false, reason: 'invalid' })
  })

  it('reports the provider unavailable when the keys cannot be fetched', async () => {
    const offline = new OidcTokenVerifier({
      keys: () => Promise.reject(new TypeError('fetch failed')),
      issuers: [ISSUER],
      audiences: [AUDIENCE],
    })

    expect(await offline.verify(await sign())).toEqual({ valid: false, reason: 'unavailable' })
  })
})

describe('FacebookGraphVerifier', () => {
  type Reply = { status: number; body?: unknown } | Error

  const makeVerifier = (debug: Reply, me: Reply = { status: 200, body: { id: '42' } }) => {
    const calls: string[] = []
    const fakeFetch = (async (input: string) => {
      const url = input
      calls.push(url)
      const reply = url.includes('/debug_token') ? debug : me
      if (reply instanceof Error) throw reply
      return new Response(JSON.stringify(reply.body ?? {}), { status: reply.status })
    }) as typeof fetch

    const verifier = new FacebookGraphVerifier({
      appId: 'app-1',
      appSecret: 'secret',
      apiVersion: 'v24.0',
      fetch: fakeFetch,
    })
    return { verifier, calls }
  }

  const validDebug = {
    status: 200,
    body: { data: { is_valid: true, app_id: 'app-1', user_id: '42' } },
  }

  it('accepts a valid token and reads the email', async () => {
    const { verifier, calls } = makeVerifier(validDebug, {
      status: 200,
      body: { id: '42', email: 'FB@Example.com' },
    })

    expect(await verifier.verify('user-token')).toEqual({
      valid: true,
      profile: { providerUserId: '42', email: 'fb@example.com' },
    })
    expect(calls[0]).toContain('https://graph.facebook.com/v24.0/debug_token')
    expect(calls[1]).toContain('appsecret_proof=')
  })

  it.each([
    ['token of another app', { data: { is_valid: true, app_id: 'other-app', user_id: '42' } }],
    ['token marked invalid', { data: { is_valid: false, app_id: 'app-1', user_id: '42' } }],
    ['no user id', { data: { is_valid: true, app_id: 'app-1' } }],
  ])('rejects a %s', async (_case, body) => {
    const { verifier } = makeVerifier({ status: 200, body })
    expect(await verifier.verify('user-token')).toEqual({ valid: false, reason: 'invalid' })
  })

  it('rejects when graph refuses the token or the ids differ', async () => {
    expect(await makeVerifier({ status: 400 }).verifier.verify('t')).toMatchObject({
      reason: 'invalid',
    })
    expect(
      await makeVerifier(validDebug, { status: 200, body: { id: '99' } }).verifier.verify('t'),
    ).toMatchObject({ reason: 'invalid' })
  })

  it('reports the provider unavailable on network errors and 5xx', async () => {
    expect(await makeVerifier(new TypeError('fetch failed')).verifier.verify('t')).toMatchObject({
      reason: 'unavailable',
    })
    expect(await makeVerifier(validDebug, { status: 503 }).verifier.verify('t')).toMatchObject({
      reason: 'unavailable',
    })
  })
})

describe('FacebookTokenVerifier', () => {
  it('sends jwt shaped tokens to limited login and the rest to graph', async () => {
    const tagged = (tag: string): SocialTokenVerifier => ({
      verify: async () => ({ valid: true, profile: { providerUserId: tag, email: null } }),
    })
    const verifier = new FacebookTokenVerifier(tagged('limited'), tagged('graph'))

    expect(await verifier.verify('aaa.bbb.ccc')).toMatchObject({
      profile: { providerUserId: 'limited' },
    })
    expect(await verifier.verify('EAAGm0PX4ZCpsBA')).toMatchObject({
      profile: { providerUserId: 'graph' },
    })
  })
})

describe('MockTokenVerifier', () => {
  const verifier = new MockTokenVerifier()

  it('accepts mock tokens and prefixes the id', async () => {
    expect(await verifier.verify('mock:player-1')).toEqual({
      valid: true,
      profile: { providerUserId: 'mock:player-1', email: null },
    })
    expect(await verifier.verify('mock:player-1:Me@Example.com')).toMatchObject({
      profile: { email: 'me@example.com' },
    })
  })

  it.each(['player-1', 'mock:', 'mock:a b', 'mock:p1:not-an-email', 'aaa.bbb.ccc'])(
    'rejects %s',
    async (token) => {
      expect(await verifier.verify(token)).toEqual({ valid: false, reason: 'invalid' })
    },
  )
})

describe('buildSocialTokenVerifiers', () => {
  const none = {
    GOOGLE_CLIENT_IDS: [],
    FACEBOOK_APP_ID: undefined,
    FACEBOOK_APP_SECRET: undefined,
    FACEBOOK_GRAPH_API_VERSION: 'v24.0',
    APPLE_CLIENT_IDS: [],
    SOCIAL_AUTH_MOCK: false,
  }

  it('leaves providers out when nothing is configured', () => {
    const { verifiers, modes } = buildSocialTokenVerifiers(none)

    expect(verifiers).toEqual({})
    expect(modes).toEqual({ GOOGLE: 'disabled', FACEBOOK: 'disabled', APPLE: 'disabled' })
  })

  it('uses the real verifier when configured and the mock for the rest', () => {
    const { verifiers, modes } = buildSocialTokenVerifiers({
      ...none,
      GOOGLE_CLIENT_IDS: ['google-client'],
      SOCIAL_AUTH_MOCK: true,
    })

    expect(modes).toEqual({ GOOGLE: 'real', FACEBOOK: 'mock', APPLE: 'mock' })
    expect(verifiers.GOOGLE).toBeInstanceOf(OidcTokenVerifier)
    expect(verifiers.FACEBOOK).toBeInstanceOf(MockTokenVerifier)
  })

  it('configures facebook and apple', () => {
    const { modes } = buildSocialTokenVerifiers({
      ...none,
      FACEBOOK_APP_ID: 'app',
      FACEBOOK_APP_SECRET: 'secret',
      APPLE_CLIENT_IDS: ['com.example.game'],
    })

    expect(modes).toEqual({ GOOGLE: 'disabled', FACEBOOK: 'real', APPLE: 'real' })
  })
})
