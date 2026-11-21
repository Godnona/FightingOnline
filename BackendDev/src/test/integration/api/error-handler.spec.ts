import { afterAll, beforeAll, describe, expect, it } from 'vitest'
import { z } from 'zod'

import { buildApp } from '@/app'
import { DomainError } from '@/use-cases/_errors/domain-error'

class UnmappedTestError extends DomainError {
  readonly code = 'UNMAPPED_TEST_ERROR'
}

describe('error handler', () => {
  const app = buildApp()

  beforeAll(async () => {
    app.post(
      '/__test/validation',
      { schema: { body: z.object({ name: z.string().min(3) }) } },
      async () => ({ ok: true }),
    )
    app.get('/__test/crash', async () => {
      throw new Error('secret internal detail')
    })
    app.get('/__test/unmapped-domain-error', async () => {
      throw new UnmappedTestError('should not leak')
    })

    await app.ready()
  })

  afterAll(async () => {
    await app.close()
  })

  it('formats Zod validation errors as 400', async () => {
    const response = await app.inject({
      method: 'POST',
      url: '/__test/validation',
      payload: { name: 'ab' },
    })

    expect(response.statusCode).toBe(400)
    expect(response.json()).toMatchObject({
      code: 'VALIDATION_ERROR',
      details: { location: 'body', issues: [{ path: '/name' }] },
    })
  })

  it('keeps 4xx errors from Fastify (malformed JSON)', async () => {
    const response = await app.inject({
      method: 'POST',
      url: '/__test/validation',
      headers: { 'content-type': 'application/json' },
      payload: '{ not json',
    })

    expect(response.statusCode).toBe(400)
    expect(response.json<{ code: string }>().code).toBe('FST_ERR_CTP_INVALID_JSON_BODY')
  })

  it('hides internal details on unexpected errors', async () => {
    const response = await app.inject({ method: 'GET', url: '/__test/crash' })

    expect(response.statusCode).toBe(500)
    expect(response.json()).toEqual({
      code: 'INTERNAL_SERVER_ERROR',
      message: 'Internal server error',
    })
  })

  it('treats domain errors without HTTP mapping as 500', async () => {
    const response = await app.inject({ method: 'GET', url: '/__test/unmapped-domain-error' })

    expect(response.statusCode).toBe(500)
    expect(response.body).not.toContain('should not leak')
  })

  it('returns 404 in the standard format for unknown routes', async () => {
    const response = await app.inject({ method: 'GET', url: '/does-not-exist' })

    expect(response.statusCode).toBe(404)
    expect(response.json()).toMatchObject({ code: 'ROUTE_NOT_FOUND' })
  })
})
