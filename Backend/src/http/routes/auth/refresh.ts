import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { refreshController } from '@/http/controllers/auth/refresh-controller'
import {
  deviceInfoSchema,
  opaqueTokenSchema,
  sessionResponseSchema,
} from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const refreshBodySchema = z.object({
  refreshToken: opaqueTokenSchema,
  deviceInfo: deviceInfoSchema,
})

export type RefreshBody = z.infer<typeof refreshBodySchema>

export async function refresh(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/refresh',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Exchange a refresh token for a new token pair (rotation)',
        description:
          'Each refresh token works once. Reusing an old one revokes the whole session. ' +
          'Clients must never send two refresh requests in parallel.',
        body: refreshBodySchema,
        response: { 200: z.object({ session: sessionResponseSchema }) },
      },
    },
    refreshController,
  )
}
