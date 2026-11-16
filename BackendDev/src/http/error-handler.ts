import type { FastifyError, FastifyReply, FastifyRequest } from 'fastify'
import {
  hasZodFastifySchemaValidationErrors,
  isResponseSerializationError,
} from 'fastify-type-provider-zod'

import {
  AccessTokenExpiredError,
  AccountBannedError,
  DisplayNameTakenError,
  EmailAlreadyInUseError,
  IdentityAlreadyLinkedError,
  IdentityNotFoundError,
  InvalidAccessTokenError,
  InvalidCredentialsError,
  InvalidPasswordResetCodeError,
  InvalidProviderTokenError,
  InvalidRefreshTokenError,
  LastIdentityError,
  ProviderAlreadyLinkedError,
  ProviderNotConfiguredError,
  ProviderUnavailableError,
} from '@/use-cases/_errors/auth-errors'
import { DomainError } from '@/use-cases/_errors/domain-error'
import {
  CannotChangeOwnRoleError,
  DisplayNameChangeTooSoonError,
  InvalidAvatarError,
  PlayerNotFoundError,
} from '@/use-cases/_errors/profile-errors'

export type ErrorResponse = {
  code: string
  message: string
  details?: unknown
}

type DomainErrorClass = abstract new (...args: never[]) => DomainError

// new domain error? add it here, this is the only place that maps them to http status
const domainErrorStatus: ReadonlyArray<readonly [DomainErrorClass, number]> = [
  [InvalidPasswordResetCodeError, 400],
  [InvalidAvatarError, 400],
  [PlayerNotFoundError, 404],
  [DisplayNameChangeTooSoonError, 409],
  [CannotChangeOwnRoleError, 409],
  [InvalidCredentialsError, 401],
  [InvalidRefreshTokenError, 401],
  [InvalidAccessTokenError, 401],
  [AccessTokenExpiredError, 401],
  [InvalidProviderTokenError, 401],
  [AccountBannedError, 403],
  [IdentityNotFoundError, 404],
  [EmailAlreadyInUseError, 409],
  [DisplayNameTakenError, 409],
  [IdentityAlreadyLinkedError, 409],
  [ProviderAlreadyLinkedError, 409],
  [LastIdentityError, 409],
  [ProviderNotConfiguredError, 503],
  [ProviderUnavailableError, 503],
]

function statusForDomainError(error: DomainError): number | undefined {
  return domainErrorStatus.find(([errorClass]) => error instanceof errorClass)?.[1]
}

function send(reply: FastifyReply, status: number, body: ErrorResponse) {
  return reply.status(status).send(body)
}

export function errorHandler(
  error: FastifyError | DomainError,
  request: FastifyRequest,
  reply: FastifyReply,
) {
  if (hasZodFastifySchemaValidationErrors(error)) {
    return send(reply, 400, {
      code: 'VALIDATION_ERROR',
      message: 'Invalid request',
      details: {
        location: error.validationContext,
        issues: error.validation.map((issue) => ({
          path: issue.instancePath,
          message: issue.message,
        })),
      },
    })
  }

  if (isResponseSerializationError(error)) {
    request.log.error({ err: error }, 'response does not match its schema')
    return send(reply, 500, { code: 'INTERNAL_SERVER_ERROR', message: 'Internal server error' })
  }

  if (error instanceof DomainError) {
    const status = statusForDomainError(error)

    if (status !== undefined) {
      return send(reply, status, {
        code: error.code,
        message: error.message,
        ...(error.details !== undefined && { details: error.details }),
      })
    }

    request.log.error({ err: error }, 'domain error without HTTP mapping')
    return send(reply, 500, { code: 'INTERNAL_SERVER_ERROR', message: 'Internal server error' })
  }

  if (error.statusCode !== undefined && error.statusCode >= 400 && error.statusCode < 500) {
    return send(reply, error.statusCode, {
      code: error.code ?? 'REQUEST_ERROR',
      message: error.message,
    })
  }

  request.log.error({ err: error }, 'unhandled error')
  return send(reply, 500, { code: 'INTERNAL_SERVER_ERROR', message: 'Internal server error' })
}

export function notFoundHandler(request: FastifyRequest, reply: FastifyReply) {
  return send(reply, 404, {
    code: 'ROUTE_NOT_FOUND',
    message: `Route ${request.method} ${request.url} not found`,
  })
}
