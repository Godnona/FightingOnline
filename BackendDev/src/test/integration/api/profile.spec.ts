import { afterAll, beforeAll, beforeEach, describe, expect, it } from 'vitest'

import { buildApp } from '@/app'
import { prisma } from '@/lib/prisma'
import { resetDatabase } from '@/test/helpers/reset-database'

type Profile = {
  id: string
  displayName: string
  avatar: string | null
  role: string
  createdAt: string
  nextDisplayNameChangeAt: string | null
}

describe('profile routes', () => {
  const app = buildApp()

  let ipCounter = 0
  let ip = ''

  const request = (
    method: 'GET' | 'PATCH' | 'POST',
    url: string,
    options: { payload?: object; accessToken?: string } = {},
  ) =>
    app.inject({
      method,
      url,
      payload: options.payload,
      headers: options.accessToken ? { authorization: `Bearer ${options.accessToken}` } : {},
      remoteAddress: ip,
    })

  const register = async (email: string, displayName: string) => {
    const response = await request('POST', '/auth/register', {
      payload: { email, password: 'a-good-password', displayName },
    })
    expect(response.statusCode).toBe(201)
    return response.json<{ user: { id: string }; session: { accessToken: string } }>()
  }

  beforeAll(async () => {
    await app.ready()
  })

  beforeEach(async () => {
    ipCounter++
    ip = `10.2.${Math.floor(ipCounter / 250)}.${(ipCounter % 250) + 1}`
    await resetDatabase(prisma)
  })

  afterAll(async () => {
    await app.close()
  })

  it('returns the own profile', async () => {
    const { session } = await register('player@example.com', 'PlayerOne')

    const response = await request('GET', '/profile', { accessToken: session.accessToken })

    expect(response.statusCode).toBe(200)
    expect(response.json<Profile>()).toMatchObject({
      displayName: 'PlayerOne',
      avatar: null,
      role: 'PLAYER',
      nextDisplayNameChangeAt: null,
    })
  })

  it('renames once, then blocks for 30 days', async () => {
    const { session } = await register('player@example.com', 'PlayerOne')
    const accessToken = session.accessToken

    const renamed = await request('PATCH', '/profile', {
      payload: { displayName: 'PlayerTwo', avatar: 'avatar_03' },
      accessToken,
    })
    expect(renamed.statusCode).toBe(200)
    const body = renamed.json<Profile>()
    expect(body).toMatchObject({ displayName: 'PlayerTwo', avatar: 'avatar_03' })
    expect(body.nextDisplayNameChangeAt).not.toBeNull()

    const again = await request('PATCH', '/profile', {
      payload: { displayName: 'PlayerThree' },
      accessToken,
    })
    expect(again.statusCode).toBe(409)
    expect(again.json()).toMatchObject({
      code: 'DISPLAY_NAME_CHANGE_TOO_SOON',
      details: { nextChangeAt: body.nextDisplayNameChangeAt },
    })

    const avatarOnly = await request('PATCH', '/profile', {
      payload: { avatar: null },
      accessToken,
    })
    expect(avatarOnly.statusCode).toBe(200)
    expect(avatarOnly.json<Profile>().avatar).toBeNull()
  })

  it('maps taken names, unknown avatars and empty bodies to errors', async () => {
    await register('other@example.com', 'TakenName')
    const { session } = await register('player@example.com', 'PlayerOne')
    const accessToken = session.accessToken

    const taken = await request('PATCH', '/profile', {
      payload: { displayName: 'takenname' },
      accessToken,
    })
    expect(taken.statusCode).toBe(409)
    expect(taken.json()).toMatchObject({ code: 'DISPLAY_NAME_TAKEN' })

    const avatar = await request('PATCH', '/profile', {
      payload: { avatar: 'avatar_99' },
      accessToken,
    })
    expect(avatar.statusCode).toBe(400)
    expect(avatar.json()).toMatchObject({ code: 'INVALID_AVATAR' })

    for (const payload of [{}, { displayName: 'no spaces allowed' }]) {
      const invalid = await request('PATCH', '/profile', { payload, accessToken })
      expect(invalid.statusCode).toBe(400)
      expect(invalid.json()).toMatchObject({ code: 'VALIDATION_ERROR' })
    }
  })

  it('shows the public profile of another player', async () => {
    const other = await register('other@example.com', 'OtherOne')
    const { session } = await register('player@example.com', 'PlayerOne')

    const response = await request('GET', `/players/${other.user.id}`, {
      accessToken: session.accessToken,
    })
    expect(response.statusCode).toBe(200)
    expect(response.json()).toEqual({ id: other.user.id, displayName: 'OtherOne', avatar: null })

    const missing = await request('GET', '/players/00000000-0000-7000-8000-000000000000', {
      accessToken: session.accessToken,
    })
    expect(missing.statusCode).toBe(404)
    expect(missing.json()).toMatchObject({ code: 'PLAYER_NOT_FOUND' })
  })

  it('requires authentication', async () => {
    expect((await request('GET', '/profile')).statusCode).toBe(401)
    expect((await request('PATCH', '/profile', { payload: { avatar: null } })).statusCode).toBe(401)
  })
})
