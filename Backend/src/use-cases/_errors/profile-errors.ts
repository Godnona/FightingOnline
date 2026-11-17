import { DomainError } from './domain-error'

export class DisplayNameChangeTooSoonError extends DomainError {
  readonly code = 'DISPLAY_NAME_CHANGE_TOO_SOON'

  constructor(nextChangeAt: Date) {
    super('The display name can be changed once every 30 days', {
      nextChangeAt: nextChangeAt.toISOString(),
    })
  }
}

export class InvalidAvatarError extends DomainError {
  readonly code = 'INVALID_AVATAR'

  constructor() {
    super('Unknown avatar')
  }
}

export class CannotChangeOwnRoleError extends DomainError {
  readonly code = 'CANNOT_CHANGE_OWN_ROLE'

  constructor() {
    super('Admins cannot change their own role, ask another admin')
  }
}

export class PlayerNotFoundError extends DomainError {
  readonly code = 'PLAYER_NOT_FOUND'

  constructor() {
    super('Player not found')
  }
}
