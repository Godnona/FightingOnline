export abstract class DomainError extends Error {
  abstract readonly code: string

  constructor(
    message: string,
    readonly details?: unknown,
  ) {
    super(message)
    this.name = new.target.name
  }
}
