import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentPublicProfile } from '@/http/presenters/profile-presenters'
import type { GetPlayerProfileParams } from '@/http/routes/profile/get-player-profile'
import { makeGetPlayerProfileUseCase } from '@/use-cases/factories/make-get-player-profile-use-case'

export async function getPlayerProfileController(
  request: FastifyRequest<{ Params: GetPlayerProfileParams }>,
  reply: FastifyReply,
) {
  const user = await makeGetPlayerProfileUseCase().execute({ playerId: request.params.playerId })
  return reply.send(presentPublicProfile(user))
}
