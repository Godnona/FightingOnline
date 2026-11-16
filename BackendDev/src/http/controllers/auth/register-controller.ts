import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentSession, presentUser } from '@/http/presenters/auth-presenters'
import type { RegisterBody } from '@/http/routes/auth/register'
import { sessionMetadata } from '@/http/utils/session-metadata'
import { makeRegisterWithEmailUseCase } from '@/use-cases/factories/make-register-with-email-use-case'

export async function registerController(
  request: FastifyRequest<{ Body: RegisterBody }>,
  reply: FastifyReply,
) {
  const { email, password, displayName, deviceInfo } = request.body

  const { user, session } = await makeRegisterWithEmailUseCase().execute({
    email,
    password,
    displayName,
    ...sessionMetadata(request, deviceInfo),
  })

  return reply.status(201).send({ user: presentUser(user), session: presentSession(session) })
}
