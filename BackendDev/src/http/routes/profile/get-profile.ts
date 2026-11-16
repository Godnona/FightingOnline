import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'

import { getProfileController } from '@/http/controllers/profile/get-profile-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { profileResponseSchema } from '@/http/schemas/profile-schemas'

export async function getProfile(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().get(
    '/profile',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['profile'],
        summary: 'Profile of the signed-in player',
        security: [{ bearerAuth: [] }],
        response: { 200: profileResponseSchema },
      },
    },
    getProfileController,
  )
}
