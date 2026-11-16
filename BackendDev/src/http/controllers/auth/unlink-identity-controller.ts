import type { FastifyReply, FastifyRequest } from 'fastify'

import type { IdentityProvider } from '@/generated/prisma/client'
import type { UnlinkIdentityParams } from '@/http/routes/auth/unlink-identity'
import { makeUnlinkIdentityUseCase } from '@/use-cases/factories/make-unlink-identity-use-case'

export async function unlinkIdentityController(
  request: FastifyRequest<{ Params: UnlinkIdentityParams }>,
  reply: FastifyReply,
) {
  await makeUnlinkIdentityUseCase().execute({
    userId: request.getCurrentUserId(),
    provider: request.params.provider.toUpperCase() as IdentityProvider,
  })
  return reply.status(204).send(null)
}
