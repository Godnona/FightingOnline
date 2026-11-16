import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { logoutAllController } from '@/http/controllers/auth/logout-all-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'

export async function logoutAll(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/logout-all',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['auth'],
        summary: 'End every session of the authenticated user',
        security: [{ bearerAuth: [] }],
        response: { 204: z.null() },
      },
    },
    logoutAllController,
  )
}
