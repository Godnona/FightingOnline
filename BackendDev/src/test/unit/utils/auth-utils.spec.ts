import { describe, expect, it } from 'vitest'

import { DisplayNameGenerator } from '@/use-cases/auth/display-name-generator'
import { InMemoryDatabase } from '@/test/helpers/in-memory/in-memory-database'
import { InMemoryUsersRepository } from '@/test/helpers/in-memory/in-memory-users-repository'
import { makeCreateUserData } from '@/test/helpers/factories/make-repository-data'
import { generateDisplayNameCandidate, isValidDisplayName } from '@/utils/display-name'
import { normalizeEmail } from '@/utils/email'
import { isCommonPassword } from '@/utils/password-policy'
import { generateOpaqueToken, hashToken } from '@/utils/secure-token'

describe('display name rules', () => {
  it.each(['abc', 'Player_1', 'a_b_c_d_e_f_g_h_', 'NEO', '123'])('accepts "%s"', (name) => {
    expect(isValidDisplayName(name)).toBe(true)
  })

  it.each(['ab', 'a'.repeat(17), 'has space', 'dash-name', 'ação', 'emoji🙂', ''])(
    'rejects "%s"',
    (name) => {
      expect(isValidDisplayName(name)).toBe(false)
    },
  )

  it('generates valid candidates', () => {
    for (let i = 0; i < 50; i++) {
      const candidate = generateDisplayNameCandidate()
      expect(candidate).toMatch(/^Player\d{6}$/)
      expect(isValidDisplayName(candidate)).toBe(true)
    }
  })
})

describe('DisplayNameGenerator', () => {
  it('skips names that are already taken', async () => {
    const users = new InMemoryUsersRepository(new InMemoryDatabase())
    await users.create(makeCreateUserData({ displayName: 'Player000001' }))
    const candidates = ['Player000001', 'PLAYER000001', 'Player000002']

    const generator = new DisplayNameGenerator(users, () => candidates.shift()!)

    expect(await generator.generate()).toBe('Player000002')
  })

  it('gives up after the maximum number of attempts', async () => {
    const users = new InMemoryUsersRepository(new InMemoryDatabase())
    await users.create(makeCreateUserData({ displayName: 'Player000001' }))

    const generator = new DisplayNameGenerator(users, () => 'Player000001', 3)

    await expect(generator.generate()).rejects.toThrow(/3 attempts/)
  })
})

describe('auth utilities', () => {
  it('normalizes emails', () => {
    expect(normalizeEmail('  Some.One@Example.COM ')).toBe('some.one@example.com')
  })

  it('flags common passwords regardless of case', () => {
    expect(isCommonPassword('Password123')).toBe(true)
    expect(isCommonPassword('correct horse battery staple')).toBe(false)
  })

  it('generates and hashes tokens', () => {
    const token = generateOpaqueToken()

    expect(token).toMatch(/^[A-Za-z0-9_-]{43}$/)
    expect(generateOpaqueToken()).not.toBe(token)
    expect(hashToken(token)).toBe(hashToken(token))
    expect(hashToken(token)).toMatch(/^[0-9a-f]{64}$/)
  })
})
