import { beforeEach, describe, expect, it } from 'vitest'

import { AccountBannedError, InvalidCredentialsError } from '@/use-cases/_errors/auth-errors'
import { FakePasswordHasher } from '@/test/helpers/fakes/fake-password-hasher'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

describe('AuthenticateWithEmailUseCase', () => {
  let ctx: AuthTestContext
  let userId: string

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    const { user } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'right-password',
      displayName: 'Player_One',
    })
    userId = user.id
  })

  it('signs in with the right password', async () => {
    const { user, session } = await ctx.login.execute({
      email: 'PLAYER@example.com',
      password: 'right-password',
    })

    expect(user.id).toBe(userId)
    expect(session.refreshToken).toBeTruthy()
    expect(ctx.db.identities[0]?.lastUsedAt).toBeInstanceOf(Date)
  })

  it('same error for wrong password and unknown email', async () => {
    const wrongPassword = ctx.login.execute({ email: 'player@example.com', password: 'nope' })
    const unknownEmail = ctx.login.execute({ email: 'ghost@example.com', password: 'nope' })

    await expect(wrongPassword).rejects.toBeInstanceOf(InvalidCredentialsError)
    await expect(unknownEmail).rejects.toBeInstanceOf(InvalidCredentialsError)
  })

  it('runs a dummy hash check for unknown emails', async () => {
    await expect(
      ctx.login.execute({ email: 'ghost@example.com', password: 'nope' }),
    ).rejects.toThrow()

    expect(ctx.passwordHasher.dummyVerifications).toBe(1)
  })

  it('blocks banned accounts after checking the password', async () => {
    ctx.db.users[0]!.status = 'BANNED'

    await expect(
      ctx.login.execute({ email: 'player@example.com', password: 'nope' }),
    ).rejects.toBeInstanceOf(InvalidCredentialsError)
    await expect(
      ctx.login.execute({ email: 'player@example.com', password: 'right-password' }),
    ).rejects.toBeInstanceOf(AccountBannedError)
  })

  it('treats deleted accounts as unknown', async () => {
    ctx.db.users[0]!.status = 'DELETED'

    await expect(
      ctx.login.execute({ email: 'player@example.com', password: 'right-password' }),
    ).rejects.toBeInstanceOf(InvalidCredentialsError)
  })

  it('upgrades outdated password hashes on sign-in', async () => {
    ctx.db.identities[0]!.passwordHash = `${FakePasswordHasher.OUTDATED_PREFIX}right-password`

    await ctx.login.execute({ email: 'player@example.com', password: 'right-password' })

    expect(ctx.db.identities[0]?.passwordHash).toBe('hashed:right-password')
  })
})
