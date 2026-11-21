import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { unlinkIdentityController } from '@/http/controllers/auth/unlink-identity-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { identityProviderParamSchema } from '@/http/schemas/auth-schemas'

export const unlinkIdentityParamsSchema = z.object({ provider: identityProviderParamSchema })

export type UnlinkIdentityParams = z.infer<typeof unlinkIdentityParamsSchema>

export async function unlinkIdentity(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().delete(
    '/identities/:provider',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['auth'],
        summary: 'Remove a sign-in method from the account',
        description: 'The last sign-in method of an account cannot be removed.',
        security: [{ bearerAuth: [] }],
        params: unlinkIdentityParamsSchema,
        response: { 204: z.null() },
      },
    },
    unlinkIdentityController,
  )
}
