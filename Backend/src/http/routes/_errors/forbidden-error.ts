export class ForbiddenError extends Error {
  readonly statusCode = 403
  readonly code = 'FORBIDDEN'

  constructor(message = 'You are not allowed to do this') {
    super(message)
    this.name = 'ForbiddenError'
  }
}
