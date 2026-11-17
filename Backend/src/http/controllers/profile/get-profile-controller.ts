import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentProfile } from '@/http/presenters/profile-presenters'
import { makeGetProfileUseCase } from '@/use-cases/factories/make-get-profile-use-case'

export async function getProfileController(request: FastifyRequest, reply: FastifyReply) {
  const user = await makeGetProfileUseCase().execute({ userId: request.getCurrentUserId() })
  return reply.send(presentProfile(user))
}
