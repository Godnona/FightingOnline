import type { FastifyReply, FastifyRequest } from 'fastify'

import type { ResetPasswordBody } from '@/http/routes/auth/reset-password'
import { makeResetPasswordUseCase } from '@/use-cases/factories/make-reset-password-use-case'

export async function resetPasswordController(
  request: FastifyRequest<{ Body: ResetPasswordBody }>,
  reply: FastifyReply,
) {
  const { email, code, newPassword } = request.body

  await makeResetPasswordUseCase().execute({ email, code, newPassword })

  return reply.status(204).send(null)
}
