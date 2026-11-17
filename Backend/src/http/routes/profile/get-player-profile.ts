import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { getPlayerProfileController } from '@/http/controllers/profile/get-player-profile-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { publicProfileResponseSchema } from '@/http/schemas/profile-schemas'

export const getPlayerProfileParamsSchema = z.object({ playerId: z.uuid() })

export type GetPlayerProfileParams = z.infer<typeof getPlayerProfileParamsSchema>

export async function getPlayerProfile(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().get(
    '/players/:playerId',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['profile'],
        summary: 'Public profile of another player',
        security: [{ bearerAuth: [] }],
        params: getPlayerProfileParamsSchema,
        response: { 200: publicProfileResponseSchema },
      },
    },
    getPlayerProfileController,
  )
}
