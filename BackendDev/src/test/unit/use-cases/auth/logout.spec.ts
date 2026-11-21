import { beforeEach, describe, expect, it } from 'vitest'

import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

describe('logout use cases', () => {
  let ctx: AuthTestContext

  beforeEach(() => {
    ctx = makeAuthTestContext()
  })

  it('logout only ends that session', async () => {
    const { user, session: phone } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'whatever123',
    })
    const { session: pc } = await ctx.login.execute({
      email: 'player@example.com',
      password: 'whatever123',
    })
    const { session: phoneRotated } = await ctx.refresh.execute({
      refreshToken: phone.refreshToken,
    })

    await ctx.logout.execute({ refreshToken: phoneRotated.refreshToken })

    const active = ctx.db.refreshTokens.filter(
      (token) => token.userId === user.id && token.revokedAt === null,
    )
    expect(active).toHaveLength(1)
    await expect(ctx.refresh.execute({ refreshToken: pc.refreshToken })).resolves.toBeTruthy()
  })

  it('logout ignores unknown tokens', async () => {
    await expect(ctx.logout.execute({ refreshToken: 'unknown' })).resolves.toBeUndefined()
  })

  it('logout all ends every session', async () => {
    const { user } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'whatever123',
    })
    await ctx.login.execute({ email: 'player@example.com', password: 'whatever123' })

    const { revokedSessions } = await ctx.logoutAll.execute({ userId: user.id })

    expect(revokedSessions).toBe(2)
    expect(ctx.db.refreshTokens.every((token) => token.revokedReason === 'LOGOUT_ALL')).toBe(true)
  })
})
