import { env } from '@/env'

export const authRateLimit = {
  max: env.AUTH_RATE_LIMIT_MAX,
  timeWindow: env.AUTH_RATE_LIMIT_WINDOW_SECONDS * 1000,
}
