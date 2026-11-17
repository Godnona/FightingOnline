import { afterAll, beforeAll, describe, expect, it } from 'vitest'

import { buildApp } from '@/app'

describe('OpenAPI documentation', () => {
  const app = buildApp()

  beforeAll(async () => {
    await app.ready()
  })

  afterAll(async () => {
    await app.close()
  })

  it('publishes the Unity-facing routes and Bearer security scheme', async () => {
    const response = await app.inject({ method: 'GET', url: '/docs/json' })

    expect(response.statusCode).toBe(200)
    const document = response.json<{
      paths: Record<string, unknown>
      components: { securitySchemes: Record<string, unknown> }
    }>()

    expect(document.paths).toHaveProperty('/auth/register')
    expect(document.paths).toHaveProperty('/auth/google')
    expect(document.paths).toHaveProperty('/auth/facebook')
    expect(document.paths).toHaveProperty('/auth/identities/{provider}')
    expect(document.paths).toHaveProperty('/profile')
    expect(document.paths).toHaveProperty('/players/{playerId}')
    expect(document.components.securitySchemes).toHaveProperty('bearerAuth')
  })
})
