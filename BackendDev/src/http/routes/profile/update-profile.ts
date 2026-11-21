import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { updateProfileController } from '@/http/controllers/profile/update-profile-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { displayNameSchema } from '@/http/schemas/auth-schemas'
import { avatarIdSchema, profileResponseSchema } from '@/http/schemas/profile-schemas'

export const updateProfileBodySchema = z
  .object({
    displayName: displayNameSchema.optional(),
    avatar: avatarIdSchema.nullable().optional(),
  })
  .refine((body) => body.displayName !== undefined || body.avatar !== undefined, {
    message: 'Send displayName, avatar or both',
  })

export type UpdateProfileBody = z.infer<typeof updateProfileBodySchema>

export async function updateProfile(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().patch(
    '/profile',
    {
      preHandler: [verifyAuthHook],
      schema: {
        tags: ['profile'],
        summary: 'Change the display name and/or the avatar',
        description:
          'The first display name change is always allowed, then once every 30 days. Send avatar null to remove it.',
        security: [{ bearerAuth: [] }],
        body: updateProfileBodySchema,
        response: { 200: profileResponseSchema },
      },
    },
    updateProfileController,
  )
}
