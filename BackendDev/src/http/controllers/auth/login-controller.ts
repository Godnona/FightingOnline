import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentSession, presentUser } from '@/http/presenters/auth-presenters'
import type { LoginBody } from '@/http/routes/auth/login'
import { sessionMetadata } from '@/http/utils/session-metadata'
import { makeAuthenticateWithEmailUseCase } from '@/use-cases/factories/make-authenticate-with-email-use-case'

export async function loginController(
  request: FastifyRequest<{ Body: LoginBody }>,
  reply: FastifyReply,
) {
  const { email, password, deviceInfo } = request.body

  const { user, session } = await makeAuthenticateWithEmailUseCase().execute({
    email,
    password,
    ...sessionMetadata(request, deviceInfo),
  })

  return reply.send({ user: presentUser(user), session: presentSession(session) })
}
