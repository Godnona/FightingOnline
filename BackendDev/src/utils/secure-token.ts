import { createHash, createHmac, randomBytes, randomInt } from 'node:crypto'

export const RESET_CODE_LENGTH = 6

export function generateResetCode(): string {
  return randomInt(0, 10 ** RESET_CODE_LENGTH)
    .toString()
    .padStart(RESET_CODE_LENGTH, '0')
}

/**
 * Six digits are easy to brute force from a plain hash, so the code is keyed with a
 * server secret. The user id keeps two users with the same code from colliding.
 */
export function hashResetCode(secret: string, userId: string, code: string): string {
  return createHmac('sha256', secret).update(`password-reset:${userId}:${code}`).digest('hex')
}

export function generateOpaqueToken(): string {
  return randomBytes(32).toString('base64url')
}

export function hashToken(token: string): string {
  return createHash('sha256').update(token).digest('hex')
}
