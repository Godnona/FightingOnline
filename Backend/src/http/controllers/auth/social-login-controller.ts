import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentSession, presentUser } from '@/http/presenters/auth-presenters'
import type { SocialLoginBody } from '@/http/routes/auth/social-login'
import { sessionMetadata } from '@/http/utils/session-metadata'
import type { SocialProvider } from '@/lib/oauth/social-token-verifier'
import { makeAuthenticateWithSocialUseCase } from '@/use-cases/factories/make-authenticate-with-social-use-case'

export function socialLoginController(provider: SocialProvider) {
  return async (request: FastifyRequest<{ Body: SocialLoginBody }>, reply: FastifyReply) => {
    const { token, deviceInfo, appleProfile } = request.body

    const { user, session, isNewUser } = await makeAuthenticateWithSocialUseCase().execute({
      provider,
      token,
      providerGivenName: provider === 'APPLE' ? appleProfile?.givenName : undefined,
      providerFamilyName: provider === 'APPLE' ? appleProfile?.familyName : undefined,
      ...sessionMetadata(request, deviceInfo),
    })

    return reply.send({ user: presentUser(user), session: presentSession(session), isNewUser })
  }
}
