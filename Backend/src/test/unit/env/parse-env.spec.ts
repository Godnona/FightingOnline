import { describe, expect, it } from 'vitest'

import { parseEnv } from '@/env'

const required = {
  DATABASE_URL: 'mongodb://localhost:27017/app',
  JWT_SECRET: 'a-secret-that-is-long-enough-for-hs256-signing',
}

describe('parseEnv', () => {
  it('applies defaults when optional variables are missing', () => {
    const env = parseEnv(required)

    expect(env).toMatchObject({
      NODE_ENV: 'development',
      HOST: '0.0.0.0',
      PORT: 3333,
      LOG_LEVEL: 'info',
      TRUST_PROXY: false,
      CORS_ORIGINS: [],
      SWAGGER_ENABLED: true,
    })
  })

  it('silences logs in tests', () => {
    expect(parseEnv({ ...required, NODE_ENV: 'test' }).LOG_LEVEL).toBe('silent')
    expect(parseEnv({ ...required, NODE_ENV: 'test', LOG_LEVEL: 'debug' }).LOG_LEVEL).toBe('debug')
  })

  it('coerces numbers, booleans and lists', () => {
    const env = parseEnv({
      ...required,
      PORT: '8080',
      TRUST_PROXY: 'true',
      CORS_ORIGINS: 'http://a.com, http://b.com ,',
    })

    expect(env.PORT).toBe(8080)
    expect(env.TRUST_PROXY).toBe(true)
    expect(env.CORS_ORIGINS).toEqual(['http://a.com', 'http://b.com'])
  })

  it('requires a MongoDB DATABASE_URL', () => {
    expect(() => parseEnv({ JWT_SECRET: required.JWT_SECRET })).toThrow(/DATABASE_URL/)
    expect(() =>
      parseEnv({ ...required, DATABASE_URL: 'postgresql://user:pass@localhost/app' }),
    ).toThrow(/DATABASE_URL/)
    expect(
      parseEnv({ ...required, DATABASE_URL: 'mongodb+srv://cluster.example.com/app' }).DATABASE_URL,
    ).toBe('mongodb+srv://cluster.example.com/app')
  })

  it('requires a JWT secret of at least 32 characters', () => {
    expect(() => parseEnv({ DATABASE_URL: required.DATABASE_URL })).toThrow(/JWT_SECRET/)
    expect(() => parseEnv({ ...required, JWT_SECRET: 'too-short' })).toThrow(/JWT_SECRET/)
  })

  it('applies auth defaults', () => {
    expect(parseEnv(required)).toMatchObject({
      ACCESS_TOKEN_TTL_SECONDS: 900,
      REFRESH_TOKEN_TTL_DAYS: 30,
      PASSWORD_RESET_CODE_TTL_MINUTES: 30,
      MAIL_TRANSPORT: 'log',
    })
  })

  it('validates SMTP settings and disables Swagger by default in production', () => {
    expect(
      parseEnv({ ...required, NODE_ENV: 'production', MAIL_TRANSPORT: 'log' }).SWAGGER_ENABLED,
    ).toBe(false)
    expect(() => parseEnv({ ...required, MAIL_TRANSPORT: 'smtp' })).toThrow(/SMTP_HOST/)
    expect(() => parseEnv({ ...required, SMTP_USER: 'user', SMTP_PASSWORD: '' })).toThrow(
      /SMTP_PASSWORD/,
    )

    expect(
      parseEnv({
        ...required,
        MAIL_TRANSPORT: 'smtp',
        SMTP_HOST: 'smtp.example.com',
        SMTP_USER: 'user',
        SMTP_PASSWORD: 'secret',
      }),
    ).toMatchObject({ MAIL_TRANSPORT: 'smtp', SMTP_PORT: 587, SMTP_SECURE: false })
  })

  it('reads the social login settings', () => {
    const defaults = parseEnv(required)
    expect(defaults).toMatchObject({
      GOOGLE_CLIENT_IDS: [],
      APPLE_CLIENT_IDS: [],
      FACEBOOK_GRAPH_API_VERSION: 'v24.0',
      SOCIAL_AUTH_MOCK: false,
    })
    expect(defaults.FACEBOOK_APP_ID).toBeUndefined()

    const env = parseEnv({
      ...required,
      GOOGLE_CLIENT_IDS: 'android-id, ios-id',
      FACEBOOK_APP_ID: '123',
      FACEBOOK_APP_SECRET: 'secret',
      SOCIAL_AUTH_MOCK: 'true',
    })
    expect(env.GOOGLE_CLIENT_IDS).toEqual(['android-id', 'ios-id'])
    expect(env.FACEBOOK_APP_ID).toBe('123')
    expect(env.SOCIAL_AUTH_MOCK).toBe(true)
  })

  it('reads the avatar ids, with placeholders when empty', () => {
    expect(parseEnv(required).AVATAR_IDS).toEqual([
      'avatar_01',
      'avatar_02',
      'avatar_03',
      'avatar_04',
      'avatar_05',
      'avatar_06',
      'avatar_07',
      'avatar_08',
    ])
    expect(parseEnv({ ...required, AVATAR_IDS: 'rx9_face, xandros_face' }).AVATAR_IDS).toEqual([
      'rx9_face',
      'xandros_face',
    ])
    expect(() => parseEnv({ ...required, AVATAR_IDS: 'bad id' })).toThrow(/AVATAR_IDS/)
  })

  it('requires the facebook app id and secret together', () => {
    expect(() => parseEnv({ ...required, FACEBOOK_APP_ID: '123' })).toThrow(/FACEBOOK_APP_SECRET/)
    expect(() => parseEnv({ ...required, FACEBOOK_APP_SECRET: 'secret' })).toThrow(
      /FACEBOOK_APP_SECRET/,
    )
    expect(
      parseEnv({ ...required, FACEBOOK_APP_ID: '', FACEBOOK_APP_SECRET: '' }).FACEBOOK_APP_ID,
    ).toBeUndefined()
  })

  it('reports all invalid variables', () => {
    expect(() =>
      parseEnv({ ...required, PORT: 'abc', NODE_ENV: 'staging', TRUST_PROXY: 'maybe' }),
    ).toThrow(/PORT[\s\S]*NODE_ENV|NODE_ENV[\s\S]*PORT/)
  })
})
