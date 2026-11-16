import { pino, type LoggerOptions } from 'pino'

import { env } from '@/env'

function loggerOptions(): LoggerOptions {
  const base: LoggerOptions = {
    level: env.LOG_LEVEL,
    redact: {
      paths: [
        'req.headers.authorization',
        '*.password',
        '*.newPassword',
        '*.refreshToken',
        '*.accessToken',
        '*.token',
      ],
      censor: '[redacted]',
    },
  }

  if (env.NODE_ENV === 'development') {
    return {
      ...base,
      transport: {
        target: 'pino-pretty',
        options: { translateTime: 'HH:MM:ss.l', ignore: 'pid,hostname' },
      },
    }
  }

  return base
}

export const logger = pino(loggerOptions())
