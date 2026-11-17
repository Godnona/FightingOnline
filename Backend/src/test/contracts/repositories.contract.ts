import { randomUUID } from 'node:crypto'

import { beforeEach, describe, expect, it } from 'vitest'

import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type { PasswordResetTokensRepository } from '@/repositories/password-reset-tokens-repository'
import type { RefreshTokensRepository } from '@/repositories/refresh-tokens-repository'
import type { UserIdentitiesRepository } from '@/repositories/user-identities-repository'
import type { UsersRepository } from '@/repositories/users-repository'
import {
  makeCreateUserData,
  makeEmailIdentityData,
  makeRefreshTokenData,
} from '@/test/helpers/factories/make-repository-data'

export type Repositories = {
  users: UsersRepository
  identities: UserIdentitiesRepository
  refreshTokens: RefreshTokensRepository
  passwordResetTokens: PasswordResetTokensRepository
}

const uniqueViolation = (...fields: string[]) =>
  expect.objectContaining({
    constructor: UniqueConstraintError,
    fields: expect.arrayContaining(fields) as string[],
  }) as unknown

export function describeRepositoriesContract(createRepositories: () => Promise<Repositories>) {
  let repos: Repositories

  beforeEach(async () => {
    repos = await createRepositories()
  })

  const createUser = (overrides?: Parameters<typeof makeCreateUserData>[0]) =>
    repos.users.create(makeCreateUserData(overrides))

  describe('UsersRepository', () => {
    it('creates a user with its first identity and defaults', async () => {
      const data = makeCreateUserData()
      const user = await repos.users.create(data)

      expect(user).toMatchObject({
        id: expect.any(String) as string,
        displayName: data.displayName,
        avatar: null,
        role: 'PLAYER',
        status: 'ACTIVE',
      })
      expect(user.identities).toHaveLength(1)
      expect(user.identities[0]).toMatchObject({
        userId: user.id,
        provider: 'EMAIL',
        providerUserId: data.identity.providerUserId,
        emailVerifiedAt: null,
      })
    })

    it('finds users by id', async () => {
      const user = await createUser()

      expect(await repos.users.findById(user.id)).toMatchObject({ id: user.id })
      expect(await repos.users.findById(randomUUID())).toBeNull()
    })

    it('finds users by display name ignoring case', async () => {
      const user = await createUser({ displayName: 'MixedCase_Name' })

      expect(await repos.users.findByDisplayName('mixedcase_name')).toMatchObject({ id: user.id })
      expect(await repos.users.findByDisplayName('someone_else')).toBeNull()
    })

    it('rejects a display name that differs only by case', async () => {
      await createUser({ displayName: 'Neo' })

      await expect(createUser({ displayName: 'NEO' })).rejects.toEqual(
        uniqueViolation('displayName'),
      )
    })

    it('updates the avatar', async () => {
      const user = await createUser()

      expect(await repos.users.update(user.id, { avatar: 'avatar_01' })).toMatchObject({
        avatar: 'avatar_01',
      })
      expect(await repos.users.update(user.id, { avatar: null })).toMatchObject({ avatar: null })
    })

    it('renames only outside the cooldown', async () => {
      const user = await createUser()
      const cooldownStart = new Date(Date.now() - 1000)

      const first = await repos.users.rename(user.id, 'Renamed_1', cooldownStart)
      expect(first).toMatchObject({ displayName: 'Renamed_1' })
      expect(first?.displayNameChangedAt).toBeInstanceOf(Date)

      expect(await repos.users.rename(user.id, 'Renamed_2', cooldownStart)).toBeNull()
      expect(await repos.users.findById(user.id)).toMatchObject({ displayName: 'Renamed_1' })

      const later = new Date(Date.now() + 60_000)
      expect(await repos.users.rename(user.id, 'Renamed_2', later)).toMatchObject({
        displayName: 'Renamed_2',
      })
    })

    it('lets only one of two parallel renames win', async () => {
      const user = await createUser()
      const cooldownStart = new Date(Date.now() - 1000)

      const results = await Promise.all([
        repos.users.rename(user.id, 'Parallel_A', cooldownStart),
        repos.users.rename(user.id, 'Parallel_B', cooldownStart),
      ])

      expect(results.filter(Boolean)).toHaveLength(1)
    })

    it('rejects renaming to a name taken by someone else', async () => {
      const taken = await createUser()
      const user = await createUser()

      await expect(
        repos.users.rename(user.id, taken.displayName.toUpperCase(), new Date()),
      ).rejects.toEqual(uniqueViolation('displayName'))
    })

    it('does not create the user if the identity exists', async () => {
      const identity = makeEmailIdentityData()
      await createUser({ identity })

      await expect(createUser({ displayName: 'Orphan', identity })).rejects.toEqual(
        uniqueViolation('provider', 'providerUserId'),
      )
      expect(await repos.users.findByDisplayName('Orphan')).toBeNull()
    })
  })

  describe('UserIdentitiesRepository', () => {
    it('links identities', async () => {
      const user = await createUser()
      const google = await repos.identities.create(user.id, {
        provider: 'GOOGLE',
        providerUserId: 'google-sub-1',
        email: 'someone@gmail.com',
        emailVerifiedAt: new Date(),
      })

      expect(await repos.identities.findById(google.id)).toMatchObject({ id: google.id })
      expect(await repos.identities.findByProvider('GOOGLE', 'google-sub-1')).toMatchObject({
        id: google.id,
        userId: user.id,
      })
      expect(await repos.identities.findByProvider('GOOGLE', 'unknown')).toBeNull()

      const identities = await repos.identities.findManyByUserId(user.id)
      expect(identities.map((identity) => identity.provider)).toEqual(['EMAIL', 'GOOGLE'])
    })

    it('allows one identity per provider', async () => {
      const user = await createUser()

      await expect(repos.identities.create(user.id, makeEmailIdentityData())).rejects.toEqual(
        uniqueViolation('userId', 'provider'),
      )
    })

    it('rejects a provider account already linked to another user', async () => {
      const first = await createUser()
      const second = await createUser()
      await repos.identities.create(first.id, { provider: 'PLAYGAMES', providerUserId: 'play-sub' })

      await expect(
        repos.identities.create(second.id, { provider: 'PLAYGAMES', providerUserId: 'play-sub' }),
      ).rejects.toEqual(uniqueViolation('provider', 'providerUserId'))
    })

    it('finds identities by email', async () => {
      const first = await createUser({
        identity: makeEmailIdentityData({ email: 'shared@example.com' }),
      })
      const second = await createUser()
      await repos.identities.create(second.id, {
        provider: 'FACEBOOK',
        providerUserId: 'fb-1',
        email: 'Shared@Example.com',
      })

      const found = await repos.identities.findManyByEmail('SHARED@example.COM')

      expect(found.map((identity) => identity.userId).sort()).toEqual([first.id, second.id].sort())
    })

    it('enforces the EMAIL identity constraints', async () => {
      const user = await createUser()

      await expect(
        repos.identities.create(user.id, {
          provider: 'GOOGLE',
          providerUserId: 'google-with-password',
          passwordHash: 'not-allowed',
        }),
      ).rejects.toThrow()

      const other = await createUser()
      await repos.identities.delete(other.identities[0]!.id)
      await expect(
        repos.identities.create(other.id, { provider: 'EMAIL', providerUserId: 'no-email' }),
      ).rejects.toThrow()
    })

    it('updates and deletes identities', async () => {
      const user = await createUser()
      const identity = user.identities[0]!
      const verifiedAt = new Date()

      const updated = await repos.identities.update(identity.id, {
        passwordHash: 'new-hash',
        emailVerifiedAt: verifiedAt,
      })
      expect(updated.passwordHash).toBe('new-hash')
      expect(updated.emailVerifiedAt?.getTime()).toBe(verifiedAt.getTime())

      await repos.identities.delete(identity.id)
      expect(await repos.identities.findById(identity.id)).toBeNull()
    })

    it('deletes an identity unless it is the last one', async () => {
      const user = await createUser()
      await repos.identities.create(user.id, { provider: 'GOOGLE', providerUserId: 'g-last' })

      expect(await repos.identities.deleteUnlessLast(user.id, 'PLAYGAMES')).toBe('not_found')
      expect(await repos.identities.deleteUnlessLast(user.id, 'GOOGLE')).toBe('deleted')
      expect(await repos.identities.deleteUnlessLast(user.id, 'EMAIL')).toBe('last_identity')

      const remaining = await repos.identities.findManyByUserId(user.id)
      expect(remaining.map((identity) => identity.provider)).toEqual(['EMAIL'])
    })

    it('keeps one identity when unlinks run at the same time', async () => {
      const user = await createUser()
      await repos.identities.create(user.id, { provider: 'GOOGLE', providerUserId: 'g-race' })

      const results = await Promise.all([
        repos.identities.deleteUnlessLast(user.id, 'EMAIL'),
        repos.identities.deleteUnlessLast(user.id, 'GOOGLE'),
      ])

      expect(results.sort()).toEqual(['deleted', 'last_identity'])
      expect(await repos.identities.findManyByUserId(user.id)).toHaveLength(1)
    })
  })

  describe('RefreshTokensRepository', () => {
    it('creates tokens and finds them by hash', async () => {
      const user = await createUser()
      const data = makeRefreshTokenData(user.id, { deviceInfo: 'Android', ipAddress: '10.0.0.1' })

      const token = await repos.refreshTokens.create(data)

      expect(await repos.refreshTokens.findByTokenHash(data.tokenHash)).toMatchObject({
        id: token.id,
        familyId: data.familyId,
        deviceInfo: 'Android',
        ipAddress: '10.0.0.1',
        revokedAt: null,
      })
      expect(await repos.refreshTokens.findByTokenHash('unknown')).toBeNull()
    })

    it('rotates a token inside its family', async () => {
      const user = await createUser()
      const current = await repos.refreshTokens.create(makeRefreshTokenData(user.id))
      const nextData = makeRefreshTokenData(user.id, { familyId: current.familyId })

      const next = await repos.refreshTokens.rotate(current.id, nextData)

      expect(next).toMatchObject({ familyId: current.familyId, revokedAt: null })
      expect(await repos.refreshTokens.findByTokenHash(current.tokenHash)).toMatchObject({
        revokedReason: 'ROTATED',
        replacedById: next?.id,
        revokedAt: expect.any(Date) as Date,
      })
    })

    it('does not rotate a revoked token', async () => {
      const user = await createUser()
      const current = await repos.refreshTokens.create(makeRefreshTokenData(user.id))
      await repos.refreshTokens.rotate(current.id, makeRefreshTokenData(user.id))

      const lateData = makeRefreshTokenData(user.id)
      expect(await repos.refreshTokens.rotate(current.id, lateData)).toBeNull()
      expect(await repos.refreshTokens.findByTokenHash(lateData.tokenHash)).toBeNull()
    })

    it('revokes a single token only once', async () => {
      const user = await createUser()
      const token = await repos.refreshTokens.create(makeRefreshTokenData(user.id))

      expect(await repos.refreshTokens.revoke(token.id, 'LOGOUT')).toBe(true)
      expect(await repos.refreshTokens.revoke(token.id, 'LOGOUT')).toBe(false)
      expect(await repos.refreshTokens.findByTokenHash(token.tokenHash)).toMatchObject({
        revokedReason: 'LOGOUT',
      })
    })

    it('revokes the active tokens of a family', async () => {
      const user = await createUser()
      const familyId = randomUUID()
      const a = await repos.refreshTokens.create(makeRefreshTokenData(user.id, { familyId }))
      await repos.refreshTokens.rotate(a.id, makeRefreshTokenData(user.id, { familyId }))
      const otherFamily = await repos.refreshTokens.create(makeRefreshTokenData(user.id))

      expect(await repos.refreshTokens.revokeFamily(familyId, 'REUSE_DETECTED')).toBe(1)
      expect(await repos.refreshTokens.findByTokenHash(a.tokenHash)).toMatchObject({
        revokedReason: 'ROTATED',
      })
      expect(await repos.refreshTokens.findByTokenHash(otherFamily.tokenHash)).toMatchObject({
        revokedAt: null,
      })
    })

    it('revokes every active token of a user', async () => {
      const user = await createUser()
      const other = await createUser()
      await repos.refreshTokens.create(makeRefreshTokenData(user.id))
      await repos.refreshTokens.create(makeRefreshTokenData(user.id))
      const othersToken = await repos.refreshTokens.create(makeRefreshTokenData(other.id))

      expect(await repos.refreshTokens.revokeAllByUserId(user.id, 'LOGOUT_ALL')).toBe(2)
      expect(await repos.refreshTokens.findByTokenHash(othersToken.tokenHash)).toMatchObject({
        revokedAt: null,
      })
    })
  })

  describe('PasswordResetTokensRepository', () => {
    const resetData = (userId: string, expiresInMs = 60 * 60 * 1000) => ({
      userId,
      tokenHash: randomUUID(),
      expiresAt: new Date(Date.now() + expiresInMs),
    })

    it('finds the active token of a user, skipping used and expired ones', async () => {
      const user = await createUser()
      const other = await createUser()
      const used = await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.markAsUsed(used.id)
      await repos.passwordResetTokens.create(resetData(user.id, -1000))

      expect(await repos.passwordResetTokens.findActiveByUserId(user.id)).toBeNull()

      const active = await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.create(resetData(other.id))

      expect(await repos.passwordResetTokens.findActiveByUserId(user.id)).toMatchObject({
        id: active.id,
        attempts: 0,
        usedAt: null,
      })
    })

    it('counts attempts up to the limit, atomically', async () => {
      const user = await createUser()
      const token = await repos.passwordResetTokens.create(resetData(user.id))

      const results = await Promise.all(
        Array.from({ length: 5 }, () => repos.passwordResetTokens.registerAttempt(token.id, 3)),
      )

      expect(results.filter(Boolean)).toHaveLength(3)
      expect(await repos.passwordResetTokens.findActiveByUserId(user.id)).toMatchObject({
        attempts: 3,
      })

      const fresh = await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.markAsUsed(fresh.id)
      expect(await repos.passwordResetTokens.registerAttempt(fresh.id, 3)).toBe(false)
    })

    it('counts the tokens created since a date', async () => {
      const user = await createUser()
      const before = new Date(Date.now() - 1000)
      await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.create(resetData(user.id))

      expect(await repos.passwordResetTokens.countCreatedSince(user.id, before)).toBe(2)
      expect(
        await repos.passwordResetTokens.countCreatedSince(user.id, new Date(Date.now() + 60_000)),
      ).toBe(0)
    })

    it('marks a token as used only once', async () => {
      const user = await createUser()
      const token = await repos.passwordResetTokens.create(resetData(user.id))

      expect(await repos.passwordResetTokens.markAsUsed(token.id)).toBe(true)
      expect(await repos.passwordResetTokens.markAsUsed(token.id)).toBe(false)
    })

    it('invalidates every pending token of a user', async () => {
      const user = await createUser()
      const used = await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.markAsUsed(used.id)
      await repos.passwordResetTokens.create(resetData(user.id))
      await repos.passwordResetTokens.create(resetData(user.id))

      expect(await repos.passwordResetTokens.invalidateAllByUserId(user.id)).toBe(2)
    })
  })
}
