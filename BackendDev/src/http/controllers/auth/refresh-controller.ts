import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentSession } from '@/http/presenters/auth-presenters'
import type { RefreshBody } from '@/http/routes/auth/refresh'
import { sessionMetadata } from '@/http/utils/session-metadata'
import { makeRefreshSessionUseCase } from '@/use-cases/factories/make-refresh-session-use-case'

export async function refreshController(
  request: FastifyRequest<{ Body: RefreshBody }>,
  reply: FastifyReply,
) {
  const { refreshToken, deviceInfo } = request.body

  const { session } = await makeRefreshSessionUseCase().execute({
    refreshToken,
    ...sessionMetadata(request, deviceInfo),
  })

  return reply.send({ session: presentSession(session) })
}
