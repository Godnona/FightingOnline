import { afterAll, beforeAll, beforeEach, describe, expect, it } from 'vitest'

import { buildApp } from '@/app'
import { prisma } from '@/lib/prisma'
import { resetDatabase } from '@/test/helpers/reset-database'

type AuthBody = { user: { id: string }; session: { accessToken: string } }

describe('admin routes', () => {
  const app = buildApp()

  let ipCounter = 0
  let ip = ''

  const request = (
    method: 'PATCH' | 'POST',
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

  const register = async (email: string) => {
    const response = await request('POST', '/auth/register', {
      payload: { email, password: 'a-good-password' },
    })
    expect(response.statusCode).toBe(201)
    return response.json<AuthBody>()
  }

  const setRole = (userId: string, role: string, accessToken: string) =>
    request('PATCH', `/admin/users/${userId}/role`, { payload: { role }, accessToken })

  beforeAll(async () => {
    await app.ready()
  })

  beforeEach(async () => {
    ipCounter++
    ip = `10.3.${Math.floor(ipCounter / 250)}.${(ipCounter % 250) + 1}`
    await resetDatabase(prisma)
  })

  afterAll(async () => {
    await app.close()
  })

  it('forbids players', async () => {
    const player = await register('player@example.com')
    const other = await register('other@example.com')

    const response = await setRole(other.user.id, 'ADMIN', player.session.accessToken)

    expect(response.statusCode).toBe(403)
    expect(response.json()).toMatchObject({ code: 'FORBIDDEN' })
  })

  it('lets an admin promote a player, and the new role works right away', async () => {
    const admin = await register('admin@example.com')
    const player = await register('player@example.com')
    const third = await register('third@example.com')
    await prisma.user.update({ where: { id: admin.user.id }, data: { role: 'ADMIN' } })

    const promoted = await setRole(player.user.id, 'ADMIN', admin.session.accessToken)
    expect(promoted.statusCode).toBe(200)
    expect(promoted.json()).toMatchObject({ id: player.user.id, role: 'ADMIN' })

    // the token was issued while still a player
    const usesNewRole = await setRole(third.user.id, 'ADMIN', player.session.accessToken)
    expect(usesNewRole.statusCode).toBe(200)
  })

  it('maps own role, unknown players and bad input to errors', async () => {
    const admin = await register('admin@example.com')
    await prisma.user.update({ where: { id: admin.user.id }, data: { role: 'ADMIN' } })
    const accessToken = admin.session.accessToken

    const own = await setRole(admin.user.id, 'PLAYER', accessToken)
    expect(own.statusCode).toBe(409)
    expect(own.json()).toMatchObject({ code: 'CANNOT_CHANGE_OWN_ROLE' })

    const missing = await setRole('00000000-0000-7000-8000-000000000000', 'ADMIN', accessToken)
    expect(missing.statusCode).toBe(404)

    const invalid = await setRole(admin.user.id, 'GOD', accessToken)
    expect(invalid.statusCode).toBe(400)
  })

  it('requires authentication', async () => {
    const response = await request(
      'PATCH',
      '/admin/users/00000000-0000-7000-8000-000000000000/role',
      {
        payload: { role: 'ADMIN' },
      },
    )

    expect(response.statusCode).toBe(401)
  })
})
