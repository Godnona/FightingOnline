import { randomInt } from 'node:crypto'

export const DISPLAY_NAME_MIN_LENGTH = 3
export const DISPLAY_NAME_MAX_LENGTH = 16

export const DISPLAY_NAME_PATTERN = /^[A-Za-z0-9_]{3,16}$/

export function isValidDisplayName(value: string): boolean {
  return DISPLAY_NAME_PATTERN.test(value)
}

export function generateDisplayNameCandidate(): string {
  return `Player${randomInt(0, 1_000_000).toString().padStart(6, '0')}`
}
