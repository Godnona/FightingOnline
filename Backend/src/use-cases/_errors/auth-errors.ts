import { DomainError } from './domain-error'

export class InvalidCredentialsError extends DomainError {
  readonly code = 'INVALID_CREDENTIALS'

  constructor() {
    super('Invalid email or password')
  }
}

export class EmailAlreadyInUseError extends DomainError {
  readonly code = 'EMAIL_ALREADY_IN_USE'

  constructor() {
    super('This email is already linked to an account')
  }
}

export class DisplayNameTakenError extends DomainError {
  readonly code = 'DISPLAY_NAME_TAKEN'

  constructor() {
    super('This display name is already taken')
  }
}

export class AccountBannedError extends DomainError {
  readonly code = 'ACCOUNT_BANNED'

  constructor() {
    super('This account is banned')
  }
}

export class InvalidRefreshTokenError extends DomainError {
  readonly code = 'INVALID_REFRESH_TOKEN'

  constructor() {
    super('Invalid or expired session, please sign in again')
  }
}

export class InvalidAccessTokenError extends DomainError {
  readonly code = 'INVALID_ACCESS_TOKEN'

  constructor() {
    super('Invalid access token')
  }
}

export class AccessTokenExpiredError extends DomainError {
  readonly code = 'ACCESS_TOKEN_EXPIRED'

  constructor() {
    super('Access token expired')
  }
}

export class InvalidPasswordResetCodeError extends DomainError {
  readonly code = 'INVALID_PASSWORD_RESET_CODE'

  constructor() {
    super('This reset code is wrong, expired or already used')
  }
}

export class InvalidProviderTokenError extends DomainError {
  readonly code = 'INVALID_PROVIDER_TOKEN'

  constructor() {
    super('The provider token is invalid or expired')
  }
}

export class ProviderNotConfiguredError extends DomainError {
  readonly code = 'PROVIDER_NOT_CONFIGURED'

  constructor(provider: string) {
    super(`Sign in with ${provider} is not available yet`)
  }
}

export class ProviderUnavailableError extends DomainError {
  readonly code = 'PROVIDER_UNAVAILABLE'

  constructor(provider: string) {
    super(`Could not reach ${provider}, try again in a moment`)
  }
}

export class IdentityAlreadyLinkedError extends DomainError {
  readonly code = 'IDENTITY_ALREADY_LINKED'

  constructor() {
    super('This provider account is already linked to another player')
  }
}

export class ProviderAlreadyLinkedError extends DomainError {
  readonly code = 'PROVIDER_ALREADY_LINKED'

  constructor() {
    super('Your account already has an identity from this provider')
  }
}

export class LastIdentityError extends DomainError {
  readonly code = 'LAST_IDENTITY'

  constructor() {
    super('The last sign-in method of an account cannot be removed')
  }
}

export class IdentityNotFoundError extends DomainError {
  readonly code = 'IDENTITY_NOT_FOUND'

  constructor() {
    super('Your account has no identity from this provider')
  }
}
