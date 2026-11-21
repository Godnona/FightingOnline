import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentIdentity } from '@/http/presenters/auth-presenters'
import type { LinkIdentityBody, LinkIdentityParams } from '@/http/routes/auth/link-identity'
import type { SocialProvider } from '@/lib/oauth/social-token-verifier'
import { makeLinkSocialIdentityUseCase } from '@/use-cases/factories/make-link-social-identity-use-case'

export async function linkIdentityController(
  request: FastifyRequest<{ Params: LinkIdentityParams; Body: LinkIdentityBody }>,
  reply: FastifyReply,
) {
  const identity = await makeLinkSocialIdentityUseCase().execute({
    userId: request.getCurrentUserId(),
    provider: request.params.provider.toUpperCase() as SocialProvider,
    token: request.body.token,
  })
  return reply.send({ identity: presentIdentity(identity) })
}
