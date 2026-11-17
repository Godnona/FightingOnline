export class RateLimitedError extends Error {
  readonly statusCode = 429
  readonly code = 'RATE_LIMITED'

  constructor(retryAfterMs: number) {
    super(`Too many requests, retry in ${Math.ceil(retryAfterMs / 1000)} seconds`)
    this.name = 'RateLimitedError'
  }
}
