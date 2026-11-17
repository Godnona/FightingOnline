import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { loginController } from '@/http/controllers/auth/login-controller'
import {
  authResponseSchema,
  currentPasswordSchema,
  deviceInfoSchema,
  emailSchema,
} from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const loginBodySchema = z.object({
  email: emailSchema,
  password: currentPasswordSchema,
  deviceInfo: deviceInfoSchema,
})

export type LoginBody = z.infer<typeof loginBodySchema>

export async function login(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/login',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Sign in with email and password',
        body: loginBodySchema,
        response: { 200: authResponseSchema },
      },
    },
    loginController,
  )
}
