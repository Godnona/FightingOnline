import type { FastifyRequest } from 'fastify'

import type { SessionMetadata } from '@/use-cases/auth/session-issuer'

const MAX_DEVICE_INFO_LENGTH = 200

export function sessionMetadata(request: FastifyRequest, deviceInfo?: string): SessionMetadata {
  const userAgent = request.headers['user-agent']
  return {
    deviceInfo: (deviceInfo ?? userAgent)?.slice(0, MAX_DEVICE_INFO_LENGTH) ?? null,
    ipAddress: request.ip,
  }
}
