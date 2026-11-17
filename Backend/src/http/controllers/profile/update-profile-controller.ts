import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentProfile } from '@/http/presenters/profile-presenters'
import type { UpdateProfileBody } from '@/http/routes/profile/update-profile'
import { makeUpdateProfileUseCase } from '@/use-cases/factories/make-update-profile-use-case'

export async function updateProfileController(
  request: FastifyRequest<{ Body: UpdateProfileBody }>,
  reply: FastifyReply,
) {
  const user = await makeUpdateProfileUseCase().execute({
    userId: request.getCurrentUserId(),
    ...request.body,
  })
  return reply.send(presentProfile(user))
}
