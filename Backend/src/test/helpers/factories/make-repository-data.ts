import { randomBytes, randomUUID } from 'node:crypto'

import type { CreateRefreshTokenData } from '@/repositories/refresh-tokens-repository'
import type { CreateIdentityData, CreateUserData } from '@/repositories/users-repository'

const uniqueSuffix = () => randomUUID().slice(0, 8)

export function makeEmailIdentityData(
  overrides: Partial<CreateIdentityData> = {},
): CreateIdentityData {
  const email = `player-${uniqueSuffix()}@example.com`
  return {
    provider: 'EMAIL',
    providerUserId: email,
    email,
    passwordHash: 'argon2id-hash-placeholder',
    ...overrides,
  }
}

export function makeCreateUserData(overrides: Partial<CreateUserData> = {}): CreateUserData {
  return {
    displayName: `Player_${uniqueSuffix()}`,
    identity: makeEmailIdentityData(),
    ...overrides,
  }
}

export function makeRefreshTokenData(
  userId: string,
  overrides: Partial<CreateRefreshTokenData> = {},
): CreateRefreshTokenData {
  return {
    userId,
    tokenHash: randomBytes(32).toString('hex'),
    familyId: randomUUID(),
    expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000),
    ...overrides,
  }
}
