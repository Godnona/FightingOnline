export class UniqueConstraintError extends Error {
  constructor(readonly fields: readonly string[]) {
    super(`Unique constraint violated on: ${fields.join(', ')}`)
    this.name = 'UniqueConstraintError'
  }

  involves(field: string) {
    return this.fields.includes(field)
  }
}
