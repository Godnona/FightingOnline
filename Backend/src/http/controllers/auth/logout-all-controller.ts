import type { FastifyReply, FastifyRequest } from 'fastify'

import { makeLogoutAllUseCase } from '@/use-cases/factories/make-logout-all-use-case'

export async function logoutAllController(request: FastifyRequest, reply: FastifyReply) {
  await makeLogoutAllUseCase().execute({ userId: request.getCurrentUserId() })
  return reply.status(204).send(null)
}
