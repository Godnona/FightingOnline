import type { FastifyReply, FastifyRequest } from 'fastify'

import type { LogoutBody } from '@/http/routes/auth/logout'
import { makeLogoutUseCase } from '@/use-cases/factories/make-logout-use-case'

export async function logoutController(
  request: FastifyRequest<{ Body: LogoutBody }>,
  reply: FastifyReply,
) {
  await makeLogoutUseCase().execute({ refreshToken: request.body.refreshToken })
  return reply.status(204).send(null)
}
