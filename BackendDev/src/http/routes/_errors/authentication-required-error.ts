export class AuthenticationRequiredError extends Error {
  readonly statusCode = 401
  readonly code = 'AUTHENTICATION_REQUIRED'

  constructor(message = 'Authentication required') {
    super(message)
    this.name = 'AuthenticationRequiredError'
  }
}
