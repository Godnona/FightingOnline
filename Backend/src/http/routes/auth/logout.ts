import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { logoutController } from '@/http/controllers/auth/logout-controller'
import { opaqueTokenSchema } from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const logoutBodySchema = z.object({
  refreshToken: opaqueTokenSchema,
})

export type LogoutBody = z.infer<typeof logoutBodySchema>

export async function logout(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/logout',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'End the session of a refresh token (always 204)',
        body: logoutBodySchema,
        response: { 204: z.null() },
      },
    },
    logoutController,
  )
}
