import { afterAll, afterEach, beforeAll, describe, expect, it, vi } from 'vitest'

import { buildApp } from '@/app'
import { prisma } from '@/lib/prisma'

describe('health routes', () => {
  const app = buildApp()

  beforeAll(async () => {
    await app.ready()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  afterAll(async () => {
    await app.close()
  })

  describe('GET /health', () => {
    it('returns ok with uptime and timestamp', async () => {
      const response = await app.inject({ method: 'GET', url: '/health' })

      expect(response.statusCode).toBe(200)
      expect(response.json()).toEqual({
        status: 'ok',
        uptime: expect.any(Number) as number,
        timestamp: expect.any(String) as string,
      })
    })

    it('returns a request id header', async () => {
      const response = await app.inject({ method: 'GET', url: '/health' })

      expect(response.headers['x-request-id']).toMatch(/^[0-9a-f-]{36}$/)
    })

    it('sets security headers', async () => {
      const response = await app.inject({ method: 'GET', url: '/health' })

      expect(response.headers['x-content-type-options']).toBe('nosniff')
    })
  })

  describe('GET /health/ready', () => {
    it('returns ok when the database is reachable', async () => {
      const response = await app.inject({ method: 'GET', url: '/health/ready' })

      expect(response.statusCode).toBe(200)
      expect(response.json()).toEqual({ status: 'ok', checks: { database: 'up' } })
    })

    it('returns 503 when the database is unreachable', async () => {
      vi.spyOn(prisma, '$queryRaw').mockRejectedValueOnce(new Error('connection refused'))

      const response = await app.inject({ method: 'GET', url: '/health/ready' })

      expect(response.statusCode).toBe(503)
      expect(response.json()).toEqual({ status: 'unavailable', checks: { database: 'down' } })
    })
  })
})
