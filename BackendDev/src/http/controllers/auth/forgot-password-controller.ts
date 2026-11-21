import type { FastifyReply, FastifyRequest } from 'fastify'

import type { ForgotPasswordBody } from '@/http/routes/auth/forgot-password'
import { makeRequestPasswordResetUseCase } from '@/use-cases/factories/make-request-password-reset-use-case'

export async function forgotPasswordController(
  request: FastifyRequest<{ Body: ForgotPasswordBody }>,
  reply: FastifyReply,
) {
  await makeRequestPasswordResetUseCase().execute({ email: request.body.email })

  return reply.status(202).send({
    message: 'If an account exists for this email, a password reset code has been sent.',
  })
}
