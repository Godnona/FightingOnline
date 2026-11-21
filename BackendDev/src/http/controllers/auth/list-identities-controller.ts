import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentIdentity } from '@/http/presenters/auth-presenters'
import { makeListIdentitiesUseCase } from '@/use-cases/factories/make-list-identities-use-case'

export async function listIdentitiesController(request: FastifyRequest, reply: FastifyReply) {
  const identities = await makeListIdentitiesUseCase().execute({
    userId: request.getCurrentUserId(),
  })
  return reply.send({ identities: identities.map(presentIdentity) })
}
