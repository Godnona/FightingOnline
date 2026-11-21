import { randomUUID } from 'node:crypto'

import cors from '@fastify/cors'
import helmet from '@fastify/helmet'
import rateLimit from '@fastify/rate-limit'
import fastify, { LogController } from 'fastify'
import {
  serializerCompiler,
  validatorCompiler,
  type ZodTypeProvider,
} from 'fastify-type-provider-zod'

import { env } from '@/env'
import { errorHandler, notFoundHandler } from '@/http/error-handler'
import { AuthenticationRequiredError } from '@/http/routes/_errors/authentication-required-error'
import { RateLimitedError } from '@/http/routes/_errors/rate-limited-error'
import { registerRoutes } from '@/http/routes'
import { logger } from '@/lib/logger'
import { prisma } from '@/lib/prisma'

function corsOrigin() {
  if (env.CORS_ORIGINS.includes('*')) return true
  return env.CORS_ORIGINS.length > 0 ? env.CORS_ORIGINS : false
}

export function buildApp() {
  const app = fastify({
    loggerInstance: logger,
    genReqId: () => randomUUID(),
    logController: new LogController({ requestIdLogLabel: 'requestId' }),
    trustProxy: env.TRUST_PROXY,
  }).withTypeProvider<ZodTypeProvider>()

  app.setValidatorCompiler(validatorCompiler)
  app.setSerializerCompiler(serializerCompiler)
  app.setErrorHandler(errorHandler)
  app.setNotFoundHandler(notFoundHandler)

  app.decorateRequest('authUser', null)
  app.decorateRequest('getCurrentUser', function () {
    if (!this.authUser) throw new AuthenticationRequiredError()
    return this.authUser
  })
  app.decorateRequest('getCurrentUserId', function () {
    return this.getCurrentUser().id
  })

  app.addHook('onRequest', async (request, reply) => {
    reply.header('x-request-id', request.id)
  })

  app.addHook('onClose', async () => {
    await prisma.$disconnect()
  })

  void app.register(helmet)
  void app.register(cors, { origin: corsOrigin() })
  void app.register(rateLimit, {
    global: false,
    errorResponseBuilder: (_request, context) => new RateLimitedError(context.ttl),
  })
  void app.register(registerRoutes)

  return app
}

export type App = ReturnType<typeof buildApp>
