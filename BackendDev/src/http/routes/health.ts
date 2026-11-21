import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { prisma } from '@/lib/prisma'

const dependencyStatus = z.enum(['up', 'down'])

export async function health(app: FastifyInstance) {
  const typedApp = app.withTypeProvider<ZodTypeProvider>()

  typedApp.get(
    '/health',
    {
      schema: {
        tags: ['health'],
        summary: 'Liveness probe',
        response: {
          200: z.object({
            status: z.literal('ok'),
            uptime: z.number(),
            timestamp: z.iso.datetime(),
          }),
        },
      },
    },
    async () => ({
      status: 'ok' as const,
      uptime: process.uptime(),
      timestamp: new Date().toISOString(),
    }),
  )

  typedApp.get(
    '/health/ready',
    {
      schema: {
        tags: ['health'],
        summary: 'Readiness probe (checks the database)',
        response: {
          200: z.object({
            status: z.literal('ok'),
            checks: z.object({ database: dependencyStatus }),
          }),
          503: z.object({
            status: z.literal('unavailable'),
            checks: z.object({ database: dependencyStatus }),
          }),
        },
      },
    },
    async (request, reply) => {
      try {
        await prisma.$queryRaw`SELECT 1`
        return { status: 'ok' as const, checks: { database: 'up' as const } }
      } catch (error) {
        request.log.error({ err: error }, 'readiness check failed: database unreachable')
        return reply
          .status(503)
          .send({ status: 'unavailable' as const, checks: { database: 'down' as const } })
      }
    },
  )
}
