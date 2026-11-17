import { beforeEach, describe, expect, it } from 'vitest'

import { AccountBannedError, InvalidRefreshTokenError } from '@/use-cases/_errors/auth-errors'
import type { AuthSession } from '@/use-cases/auth/session-issuer'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'
import { hashToken } from '@/utils/secure-token'

describe('RefreshSessionUseCase', () => {
  let ctx: AuthTestContext
  let session: AuthSession

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    ;({ session } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'whatever123',
    }))
  })

  const stored = (refreshToken: string) =>
    ctx.repositories.refreshTokens.findByTokenHash(hashToken(refreshToken))

  it('rotates the refresh token', async () => {
    const first = await stored(session.refreshToken)

    const { session: next } = await ctx.refresh.execute({ refreshToken: session.refreshToken })

    expect(next.refreshToken).not.toBe(session.refreshToken)
    expect(await stored(session.refreshToken)).toMatchObject({ revokedReason: 'ROTATED' })
    const rotated = await stored(next.refreshToken)
    expect(rotated).toMatchObject({ familyId: first?.familyId, revokedAt: null })
    expect(rotated!.expiresAt.getTime()).toBeGreaterThanOrEqual(first!.expiresAt.getTime())
  })

  it('revokes the family when an old token is reused', async () => {
    const { session: next } = await ctx.refresh.execute({ refreshToken: session.refreshToken })

    await expect(
      ctx.refresh.execute({ refreshToken: session.refreshToken }),
    ).rejects.toBeInstanceOf(InvalidRefreshTokenError)

    expect(await stored(next.refreshToken)).toMatchObject({ revokedReason: 'REUSE_DETECTED' })
    await expect(ctx.refresh.execute({ refreshToken: next.refreshToken })).rejects.toBeInstanceOf(
      InvalidRefreshTokenError,
    )
    expect(ctx.warnings).toHaveLength(1)
  })

  it('handles a concurrent rotation as reuse', async () => {
    const token = await stored(session.refreshToken)
    const refreshTokens = ctx.repositories.refreshTokens
    const originalFind = refreshTokens.findByTokenHash.bind(refreshTokens)
    refreshTokens.findByTokenHash = async (hash) => {
      const result = await originalFind(hash)
      await refreshTokens.revoke(token!.id, 'ROTATED')
      return result
    }

    await expect(
      ctx.refresh.execute({ refreshToken: session.refreshToken }),
    ).rejects.toBeInstanceOf(InvalidRefreshTokenError)
    expect(ctx.warnings).toHaveLength(1)
  })

  it('rejects unknown, expired and logged out tokens', async () => {
    await expect(ctx.refresh.execute({ refreshToken: 'unknown' })).rejects.toBeInstanceOf(
      InvalidRefreshTokenError,
    )

    ctx.db.refreshTokens[0]!.expiresAt = new Date(Date.now() - 1000)
    await expect(
      ctx.refresh.execute({ refreshToken: session.refreshToken }),
    ).rejects.toBeInstanceOf(InvalidRefreshTokenError)

    ctx.db.refreshTokens[0]!.expiresAt = new Date(Date.now() + 60_000)
    await ctx.logout.execute({ refreshToken: session.refreshToken })
    await expect(
      ctx.refresh.execute({ refreshToken: session.refreshToken }),
    ).rejects.toBeInstanceOf(InvalidRefreshTokenError)
    expect(ctx.warnings).toHaveLength(0)
  })

  it('rejects banned users', async () => {
    ctx.db.users[0]!.status = 'BANNED'

    await expect(
      ctx.refresh.execute({ refreshToken: session.refreshToken }),
    ).rejects.toBeInstanceOf(AccountBannedError)
    expect(await stored(session.refreshToken)).toMatchObject({ revokedReason: 'ADMIN' })
  })
})
