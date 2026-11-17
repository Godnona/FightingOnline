import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { resetPasswordController } from '@/http/controllers/auth/reset-password-controller'
import { emailSchema, newPasswordSchema, resetCodeSchema } from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'

export const resetPasswordBodySchema = z.object({
  email: emailSchema,
  code: resetCodeSchema,
  newPassword: newPasswordSchema,
})

export type ResetPasswordBody = z.infer<typeof resetPasswordBodySchema>

export async function resetPassword(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().post(
    '/password/reset',
    {
      config: { rateLimit: authRateLimit },
      schema: {
        tags: ['auth'],
        summary: 'Set a new password with the code from the reset email',
        description:
          'A code allows 5 attempts, then the player needs a new one. Signs the user out of every device.',
        body: resetPasswordBodySchema,
        response: { 204: z.null() },
      },
    },
    resetPasswordController,
  )
}
