import { beforeEach, describe, expect, it } from 'vitest'

import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import { DisplayNameTakenError, EmailAlreadyInUseError } from '@/use-cases/_errors/auth-errors'
import { DisplayNameGenerator } from '@/use-cases/auth/display-name-generator'
import { RegisterWithEmailUseCase } from '@/use-cases/auth/register-with-email'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'
import { hashToken } from '@/utils/secure-token'

describe('RegisterWithEmailUseCase', () => {
  let ctx: AuthTestContext

  beforeEach(() => {
    ctx = makeAuthTestContext()
  })

  it('creates the user and a session', async () => {
    const { user, session } = await ctx.register.execute({
      email: '  New.Player@Example.COM ',
      password: 'correct horse battery',
      displayName: 'NewPlayer',
      deviceInfo: 'Android 14',
      ipAddress: '10.0.0.1',
    })

    expect(user).toMatchObject({ displayName: 'NewPlayer', role: 'PLAYER', status: 'ACTIVE' })
    expect(user).not.toHaveProperty('identities')

    const [identity] = ctx.db.identities
    expect(identity).toMatchObject({
      provider: 'EMAIL',
      providerUserId: 'new.player@example.com',
      email: 'new.player@example.com',
      passwordHash: 'hashed:correct horse battery',
      emailVerifiedAt: null,
    })

    const stored = await ctx.repositories.refreshTokens.findByTokenHash(
      hashToken(session.refreshToken),
    )
    expect(stored).toMatchObject({
      userId: user.id,
      deviceInfo: 'Android 14',
      ipAddress: '10.0.0.1',
    })
    expect(session.accessToken.split('.')).toHaveLength(3)
  })

  it('generates a display name when none is given', async () => {
    const { user } = await ctx.register.execute({ email: 'a@example.com', password: 'whatever123' })

    expect(user.displayName).toMatch(/^Player\d{6}$/)
  })

  it('rejects an email already in use', async () => {
    await ctx.register.execute({ email: 'taken@example.com', password: 'whatever123' })

    await expect(
      ctx.register.execute({ email: 'TAKEN@example.com', password: 'whatever123' }),
    ).rejects.toBeInstanceOf(EmailAlreadyInUseError)
  })

  it('rejects an email used by a social login', async () => {
    const { user } = await ctx.register.execute({ email: 'a@example.com', password: 'whatever123' })
    await ctx.repositories.identities.create(user.id, {
      provider: 'GOOGLE',
      providerUserId: 'google-sub',
      email: 'social@example.com',
    })

    await expect(
      ctx.register.execute({ email: 'social@example.com', password: 'whatever123' }),
    ).rejects.toBeInstanceOf(EmailAlreadyInUseError)
  })

  it('rejects a display name already taken, ignoring case', async () => {
    await ctx.register.execute({
      email: 'a@example.com',
      password: 'whatever123',
      displayName: 'Neo',
    })

    await expect(
      ctx.register.execute({ email: 'b@example.com', password: 'whatever123', displayName: 'neo' }),
    ).rejects.toBeInstanceOf(DisplayNameTakenError)
  })

  it('retries when the generated name gets taken', async () => {
    const names = ['Player000001', 'Player000002']
    const users = ctx.repositories.users
    let failNextCreate = true
    const originalCreate = users.create.bind(users)
    users.create = async (data) => {
      if (failNextCreate) {
        failNextCreate = false
        throw new UniqueConstraintError(['displayName'])
      }
      return originalCreate(data)
    }
    const useCase = new RegisterWithEmailUseCase(
      users,
      ctx.repositories.identities,
      ctx.passwordHasher,
      new DisplayNameGenerator(users, () => names.shift() ?? 'Player999999'),
      ctx.sessions,
    )

    const { user } = await useCase.execute({ email: 'race@example.com', password: 'whatever123' })

    expect(user.displayName).toBe('Player000002')
  })

  it('returns EmailAlreadyInUseError on a duplicate identity', async () => {
    const users = ctx.repositories.users
    users.create = async () => {
      throw new UniqueConstraintError(['provider', 'providerUserId'])
    }

    await expect(
      ctx.register.execute({ email: 'race@example.com', password: 'whatever123' }),
    ).rejects.toBeInstanceOf(EmailAlreadyInUseError)
  })
})
