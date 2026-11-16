import assert from 'node:assert/strict'
import { once } from 'node:events'
import { createServer } from 'node:http'
import test from 'node:test'

import { createApp } from '../src/app.js'

async function withServer(run) {
  const server = createServer(createApp({ secret: 'test-secret-that-is-long-enough-for-tests' }))
  server.listen(0, '127.0.0.1')
  await once(server, 'listening')
  const address = server.address()
  try {
    await run(`http://127.0.0.1:${address.port}`)
  } finally {
    server.close()
    await once(server, 'close')
  }
}

test('health check confirms no database is used', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/health/ready`)
    const body = await response.json()
    assert.equal(response.status, 200)
    assert.equal(body.status, 'ok')
    assert.equal(body.database, 'not-used')
    assert.match(body.timestamp, /^\d{4}-\d{2}-\d{2}T/)
  })
})

test('login matches the Unity auth response contract', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: 'player1@ripptopia.demo', password: 'Demo123!' }),
    })
    const body = await response.json()

    assert.equal(response.status, 200)
    assert.equal(body.user.id, '11111111-1111-4111-8111-111111111111')
    assert.equal(body.user.displayName, 'PlayerOne')
    assert.equal(body.session.tokenType, 'Bearer')
    assert.match(body.session.accessToken, /^[^.]+\.[^.]+\.[^.]+$/)
    assert.ok(body.session.refreshToken)
  })
})

test('invalid credentials use the Unity error contract', async () => {
  await withServer(async (baseUrl) => {
    const response = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: 'player1@ripptopia.demo', password: 'wrong' }),
    })

    assert.equal(response.status, 401)
    assert.deepEqual(await response.json(), {
      code: 'INVALID_CREDENTIALS',
      message: 'Email or password is incorrect.',
    })
  })
})

test('issued access token can read the current profile', async () => {
  await withServer(async (baseUrl) => {
    const loginResponse = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: 'player2@ripptopia.demo', password: 'Demo123!' }),
    })
    const login = await loginResponse.json()

    const profileResponse = await fetch(`${baseUrl}/profile`, {
      headers: { authorization: `Bearer ${login.session.accessToken}` },
    })
    const profile = await profileResponse.json()

    assert.equal(profileResponse.status, 200)
    assert.equal(profile.id, login.user.id)
    assert.equal(profile.displayName, 'PlayerTwo')
  })
})

test('refresh tokens rotate and cannot be reused', async () => {
  await withServer(async (baseUrl) => {
    const loginResponse = await fetch(`${baseUrl}/auth/login`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ email: 'player1@ripptopia.demo', password: 'Demo123!' }),
    })
    const login = await loginResponse.json()
    const request = () => fetch(`${baseUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ refreshToken: login.session.refreshToken }),
    })

    const first = await request()
    const second = await request()
    assert.equal(first.status, 200)
    assert.ok((await first.json()).session.refreshToken)
    assert.equal(second.status, 401)
    assert.equal((await second.json()).code, 'INVALID_REFRESH_TOKEN')
  })
})
