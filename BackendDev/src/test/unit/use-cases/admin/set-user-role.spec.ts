import { beforeEach, describe, expect, it } from 'vitest'

import type { User } from '@/generated/prisma/client'
import { CannotChangeOwnRoleError, PlayerNotFoundError } from '@/use-cases/_errors/profile-errors'
import { SetUserRoleUseCase } from '@/use-cases/admin/set-user-role'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

describe('SetUserRoleUseCase', () => {
  let ctx: AuthTestContext
  let setRole: SetUserRoleUseCase
  let player: User

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    setRole = new SetUserRoleUseCase(ctx.repositories.users)
    ;({ user: player } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'a-good-password',
    }))
  })

  it('promotes and demotes a player', async () => {
    const promoted = await setRole.execute({ userId: player.id, role: 'ADMIN', actorId: null })
    expect(promoted.role).toBe('ADMIN')

    const demoted = await setRole.execute({ userId: player.id, role: 'PLAYER', actorId: 'admin' })
    expect(demoted.role).toBe('PLAYER')
  })

  it('does not let an admin change their own role', async () => {
    await expect(
      setRole.execute({ userId: player.id, role: 'PLAYER', actorId: player.id }),
    ).rejects.toBeInstanceOf(CannotChangeOwnRoleError)
  })

  it('reports unknown and deleted players', async () => {
    await expect(
      setRole.execute({ userId: 'missing', role: 'ADMIN', actorId: null }),
    ).rejects.toBeInstanceOf(PlayerNotFoundError)

    ctx.db.users[0]!.status = 'DELETED'
    await expect(
      setRole.execute({ userId: player.id, role: 'ADMIN', actorId: null }),
    ).rejects.toBeInstanceOf(PlayerNotFoundError)
  })
})
