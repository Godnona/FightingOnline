import { beforeEach, describe, expect, it } from 'vitest'

import type { User } from '@/generated/prisma/client'
import { DisplayNameTakenError } from '@/use-cases/_errors/auth-errors'
import {
  DisplayNameChangeTooSoonError,
  InvalidAvatarError,
  PlayerNotFoundError,
} from '@/use-cases/_errors/profile-errors'
import { nextDisplayNameChangeAt } from '@/use-cases/profile/display-name-policy'
import { GetPlayerProfileUseCase } from '@/use-cases/profile/get-player-profile'
import { GetProfileUseCase } from '@/use-cases/profile/get-profile'
import { UpdateProfileUseCase } from '@/use-cases/profile/update-profile'
import { type AuthTestContext, makeAuthTestContext } from '@/test/helpers/make-auth-test-context'

const DAY_MS = 24 * 60 * 60 * 1000
const AVATARS = ['avatar_01', 'avatar_02']

describe('profile use cases', () => {
  let ctx: AuthTestContext
  let user: User
  let updateProfile: UpdateProfileUseCase

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    ;({ user } = await ctx.register.execute({
      email: 'player@example.com',
      password: 'a-good-password',
      displayName: 'FirstName',
    }))
    updateProfile = new UpdateProfileUseCase(ctx.repositories.users, AVATARS)
  })

  const stored = () => ctx.db.users.find((item) => item.id === user.id)!

  describe('UpdateProfileUseCase', () => {
    it('allows the first rename right away and then starts the cooldown', async () => {
      const renamed = await updateProfile.execute({ userId: user.id, displayName: 'SecondName' })

      expect(renamed.displayName).toBe('SecondName')
      expect(renamed.displayNameChangedAt).toBeInstanceOf(Date)
      expect(nextDisplayNameChangeAt(renamed)?.getTime()).toBeCloseTo(Date.now() + 30 * DAY_MS, -4)

      const second = updateProfile.execute({ userId: user.id, displayName: 'ThirdName' })
      await expect(second).rejects.toBeInstanceOf(DisplayNameChangeTooSoonError)
      await expect(second).rejects.toMatchObject({
        details: { nextChangeAt: expect.any(String) as string },
      })
    })

    it('allows a new rename after 30 days', async () => {
      await updateProfile.execute({ userId: user.id, displayName: 'SecondName' })
      stored().displayNameChangedAt = new Date(Date.now() - 31 * DAY_MS)

      const renamed = await updateProfile.execute({ userId: user.id, displayName: 'ThirdName' })

      expect(renamed.displayName).toBe('ThirdName')
    })

    it('does not count sending the same name as a rename', async () => {
      const result = await updateProfile.execute({ userId: user.id, displayName: 'FirstName' })

      expect(result.displayNameChangedAt).toBeNull()
    })

    it('lets the player change only the letter case of the name', async () => {
      const renamed = await updateProfile.execute({ userId: user.id, displayName: 'firstname' })

      expect(renamed.displayName).toBe('firstname')
    })

    it('refuses a name taken by another player, ignoring case', async () => {
      await ctx.register.execute({
        email: 'other@example.com',
        password: 'a-good-password',
        displayName: 'TakenName',
      })

      await expect(
        updateProfile.execute({ userId: user.id, displayName: 'takenNAME' }),
      ).rejects.toBeInstanceOf(DisplayNameTakenError)
      expect(stored().displayNameChangedAt).toBeNull()
    })

    it('sets and removes the avatar without touching the name cooldown', async () => {
      const withAvatar = await updateProfile.execute({ userId: user.id, avatar: 'avatar_02' })
      expect(withAvatar.avatar).toBe('avatar_02')

      const without = await updateProfile.execute({ userId: user.id, avatar: null })
      expect(without.avatar).toBeNull()
      expect(without.displayNameChangedAt).toBeNull()
    })

    it('rejects unknown avatars before changing anything', async () => {
      await expect(
        updateProfile.execute({ userId: user.id, displayName: 'NewName', avatar: 'avatar_99' }),
      ).rejects.toBeInstanceOf(InvalidAvatarError)
      expect(stored().displayName).toBe('FirstName')
    })

    it('changes name and avatar together', async () => {
      const result = await updateProfile.execute({
        userId: user.id,
        displayName: 'NewName',
        avatar: 'avatar_01',
      })

      expect(result).toMatchObject({ displayName: 'NewName', avatar: 'avatar_01' })
    })
  })

  describe('GetProfileUseCase and GetPlayerProfileUseCase', () => {
    it('returns the player', async () => {
      const own = await new GetProfileUseCase(ctx.repositories.users).execute({ userId: user.id })
      const other = await new GetPlayerProfileUseCase(ctx.repositories.users).execute({
        playerId: user.id,
      })

      expect(own.id).toBe(user.id)
      expect(other.displayName).toBe('FirstName')
    })

    it('hides unknown and deleted players', async () => {
      const getPlayer = new GetPlayerProfileUseCase(ctx.repositories.users)

      await expect(
        getPlayer.execute({ playerId: '00000000-0000-7000-8000-000000000000' }),
      ).rejects.toBeInstanceOf(PlayerNotFoundError)

      stored().status = 'DELETED'
      await expect(getPlayer.execute({ playerId: user.id })).rejects.toBeInstanceOf(
        PlayerNotFoundError,
      )
    })
  })
})
