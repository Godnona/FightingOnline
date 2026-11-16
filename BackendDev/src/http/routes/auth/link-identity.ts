import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { linkIdentityController } from '@/http/controllers/auth/link-identity-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import {
  identityResponseSchema,
  providerTokenSchema,
  socialProviderParamSchema,
} from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const linkIdentityParamsSchema = z.object({ provider: socialProviderParamSchema })
export const linkIdentityBodySchema = z.object({ token: providerTokenSchema })

export type LinkIdentityParams = z.infer<typeof linkIdentityParamsSchema>
export type LinkIdentityBody = z.infer<typeof linkIdentityBodySchema>

export async function linkIdentity(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/identities/:provider',
    {
      preHandler: [verifyAuthHook],
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Link a Google, Facebook or Apple account to the signed-in player',
        description: 'Linking the same provider account again is a no-op.',
        security: [{ bearerAuth: [] }],
        params: linkIdentityParamsSchema,
        body: linkIdentityBodySchema,
        response: { 200: z.object({ identity: identityResponseSchema }) },
      },
    },
    linkIdentityController,
  )
}
