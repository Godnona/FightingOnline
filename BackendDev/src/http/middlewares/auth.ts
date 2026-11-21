import type { FastifyRequest } from 'fastify'

import { AuthenticationRequiredError } from '@/http/routes/_errors/authentication-required-error'
import { makeAuthenticateAccessTokenUseCase } from '@/use-cases/factories/make-authenticate-access-token-use-case'

const BEARER_PATTERN = /^Bearer\s+(\S+)$/i

export async function verifyAuthHook(request: FastifyRequest) {
  const accessToken = BEARER_PATTERN.exec(request.headers.authorization ?? '')?.[1]
  if (!accessToken) throw new AuthenticationRequiredError()

  request.authUser = await makeAuthenticateAccessTokenUseCase().execute({ accessToken })
}
