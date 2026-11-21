import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { listIdentitiesController } from '@/http/controllers/auth/list-identities-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { identityResponseSchema } from '@/http/schemas/auth-schemas'

export async function listIdentities(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().get(
    '/identities',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['auth'],
        summary: 'List the sign-in methods linked to the account',
        security: [{ bearerAuth: [] }],
        response: { 200: z.object({ identities: z.array(identityResponseSchema) }) },
      },
    },
    listIdentitiesController,
  )
}
