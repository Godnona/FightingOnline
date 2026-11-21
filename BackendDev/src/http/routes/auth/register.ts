import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { registerController } from '@/http/controllers/auth/register-controller'
import {
  authResponseSchema,
  deviceInfoSchema,
  displayNameSchema,
  emailSchema,
  newPasswordSchema,
} from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const registerBodySchema = z.object({
  email: emailSchema,
  password: newPasswordSchema,
  displayName: displayNameSchema.optional(),
  deviceInfo: deviceInfoSchema,
})

export type RegisterBody = z.infer<typeof registerBodySchema>

export async function register(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/register',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Create an account with email and password',
        body: registerBodySchema,
        response: { 201: authResponseSchema },
      },
    },
    registerController,
  )
}
