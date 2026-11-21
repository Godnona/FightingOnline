import { beforeEach, describe, expect, it } from 'vitest'

import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import {
  AccountBannedError,
  EmailAlreadyInUseError,
  IdentityAlreadyLinkedError,
  IdentityNotFoundError,
  InvalidCredentialsError,
  InvalidProviderTokenError,
  LastIdentityError,
  ProviderAlreadyLinkedError,
  ProviderNotConfiguredError,
  ProviderUnavailableError,
} from '@/use-cases/_errors/auth-errors'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

describe('social identities', () => {
  let ctx: AuthTestContext

  beforeEach(() => {
    ctx = makeAuthTestContext()
  })

  const googleLogin = (token: string) => ctx.socialLogin.execute({ provider: 'GOOGLE', token })

  const registerEmailUser = (email = 'player@example.com') =>
    ctx.register.execute({ email, password: 'a-good-password' })

  describe('AuthenticateWithSocialUseCase', () => {
    it('creates the account on the first sign-in', async () => {
      const { user, session, isNewUser } = await googleLogin('mock:g1:Player@Gmail.com')

      expect(isNewUser).toBe(true)
      expect(user.displayName).toMatch(/^Player\d{6}$/)
      expect(session.refreshToken).toBeTruthy()
      expect(ctx.db.identities).toHaveLength(1)
      expect(ctx.db.identities[0]).toMatchObject({
        userId: user.id,
        provider: 'GOOGLE',
        providerUserId: 'mock:g1',
        email: 'player@gmail.com',
        passwordHash: null,
      })
      expect(ctx.db.identities[0]?.emailVerifiedAt).toBeInstanceOf(Date)
    })

    it('signs in to the same account afterwards', async () => {
      const first = await googleLogin('mock:g1')
      const second = await googleLogin('mock:g1')

      expect(second.isNewUser).toBe(false)
      expect(second.user.id).toBe(first.user.id)
      expect(ctx.db.users).toHaveLength(1)
      expect(ctx.db.identities[0]?.lastUsedAt).toBeInstanceOf(Date)
    })

    it('keeps providers apart even with the same provider user id', async () => {
      const google = await googleLogin('mock:same-id')
      const facebook = await ctx.socialLogin.execute({
        provider: 'FACEBOOK',
        token: 'mock:same-id',
      })

      expect(facebook.user.id).not.toBe(google.user.id)
    })

    it('refuses an email that already belongs to an account', async () => {
      await registerEmailUser('taken@example.com')

      await expect(googleLogin('mock:g1:TAKEN@example.com')).rejects.toBeInstanceOf(
        EmailAlreadyInUseError,
      )
      expect(ctx.db.users).toHaveLength(1)
    })

    it('rejects invalid tokens', async () => {
      await expect(googleLogin('not-a-mock-token')).rejects.toBeInstanceOf(
        InvalidProviderTokenError,
      )
    })

    it('reports providers that are not configured', async () => {
      await expect(
        ctx.socialLogin.execute({ provider: 'APPLE', token: 'mock:a1' }),
      ).rejects.toBeInstanceOf(ProviderNotConfiguredError)
    })

    it('reports providers that could not be reached', async () => {
      ctx.socialVerifiers.GOOGLE = {
        verify: async () => ({ valid: false, reason: 'unavailable' }),
      }

      await expect(googleLogin('mock:g1')).rejects.toBeInstanceOf(ProviderUnavailableError)
    })

    it('blocks banned and deleted accounts', async () => {
      await googleLogin('mock:g1')

      ctx.db.users[0]!.status = 'BANNED'
      await expect(googleLogin('mock:g1')).rejects.toBeInstanceOf(AccountBannedError)

      ctx.db.users[0]!.status = 'DELETED'
      await expect(googleLogin('mock:g1')).rejects.toBeInstanceOf(InvalidCredentialsError)
    })

    it('signs in when a parallel request created the identity first', async () => {
      const { users } = ctx.repositories
      const originalCreate = users.create.bind(users)
      users.create = async (data) => {
        await originalCreate({ ...data, displayName: 'FasterRequest' })
        throw new UniqueConstraintError(['provider', 'providerUserId'])
      }

      const { user, isNewUser } = await googleLogin('mock:g1')

      expect(isNewUser).toBe(false)
      expect(user.displayName).toBe('FasterRequest')
    })
  })

  describe('LinkSocialIdentityUseCase', () => {
    it('links a provider to the signed-in player', async () => {
      const { user } = await registerEmailUser()

      const identity = await ctx.linkIdentity.execute({
        userId: user.id,
        provider: 'GOOGLE',
        token: 'mock:g1:player@example.com',
      })

      expect(identity).toMatchObject({ userId: user.id, provider: 'GOOGLE' })
      const { user: signedIn } = await googleLogin('mock:g1')
      expect(signedIn.id).toBe(user.id)
    })

    it('is a no-op when the identity is already linked to the same player', async () => {
      const { user } = await registerEmailUser()
      const link = () =>
        ctx.linkIdentity.execute({ userId: user.id, provider: 'GOOGLE', token: 'mock:g1' })

      const first = await link()
      const second = await link()

      expect(second.id).toBe(first.id)
    })

    it('refuses a provider account linked to another player', async () => {
      await googleLogin('mock:g1')
      const { user } = await registerEmailUser()

      await expect(
        ctx.linkIdentity.execute({ userId: user.id, provider: 'GOOGLE', token: 'mock:g1' }),
      ).rejects.toBeInstanceOf(IdentityAlreadyLinkedError)
    })

    it('refuses a second account of the same provider', async () => {
      const { user } = await registerEmailUser()
      await ctx.linkIdentity.execute({ userId: user.id, provider: 'GOOGLE', token: 'mock:g1' })

      await expect(
        ctx.linkIdentity.execute({ userId: user.id, provider: 'GOOGLE', token: 'mock:g2' }),
      ).rejects.toBeInstanceOf(ProviderAlreadyLinkedError)
    })

    it('refuses an email owned by another player', async () => {
      await registerEmailUser('other@example.com')
      const { user } = await registerEmailUser('me@example.com')

      await expect(
        ctx.linkIdentity.execute({
          userId: user.id,
          provider: 'FACEBOOK',
          token: 'mock:f1:other@example.com',
        }),
      ).rejects.toBeInstanceOf(EmailAlreadyInUseError)
    })
  })

  describe('UnlinkIdentityUseCase and ListIdentitiesUseCase', () => {
    it('lists and removes identities but never the last one', async () => {
      const { user } = await registerEmailUser()
      await ctx.linkIdentity.execute({ userId: user.id, provider: 'GOOGLE', token: 'mock:g1' })

      const listed = await ctx.listIdentities.execute({ userId: user.id })
      expect(listed.map((identity) => identity.provider)).toEqual(['EMAIL', 'GOOGLE'])

      await ctx.unlinkIdentity.execute({ userId: user.id, provider: 'EMAIL' })
      await expect(
        ctx.unlinkIdentity.execute({ userId: user.id, provider: 'GOOGLE' }),
      ).rejects.toBeInstanceOf(LastIdentityError)

      const remaining = await ctx.listIdentities.execute({ userId: user.id })
      expect(remaining.map((identity) => identity.provider)).toEqual(['GOOGLE'])
    })

    it('reports a provider the player does not have', async () => {
      const { user } = await registerEmailUser()

      await expect(
        ctx.unlinkIdentity.execute({ userId: user.id, provider: 'APPLE' }),
      ).rejects.toBeInstanceOf(IdentityNotFoundError)
    })
  })
})
