import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { forgotPasswordController } from '@/http/controllers/auth/forgot-password-controller'
import { emailSchema } from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const forgotPasswordBodySchema = z.object({
  email: emailSchema,
})

export type ForgotPasswordBody = z.infer<typeof forgotPasswordBodySchema>

export async function forgotPassword(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/password/forgot',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Email a password reset code (same response whether the account exists or not)',
        body: forgotPasswordBodySchema,
        response: { 202: z.object({ message: z.string() }) },
      },
    },
    forgotPasswordController,
  )
}
