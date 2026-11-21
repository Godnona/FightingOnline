import type { User } from '@/generated/prisma/client'

export const DISPLAY_NAME_CHANGE_COOLDOWN_DAYS = 30

const COOLDOWN_MS = DISPLAY_NAME_CHANGE_COOLDOWN_DAYS * 24 * 60 * 60 * 1000

/** null when the player can rename right now (the first rename is always allowed) */
export function nextDisplayNameChangeAt(
  user: Pick<User, 'displayNameChangedAt'>,
  now = new Date(),
): Date | null {
  if (!user.displayNameChangedAt) return null
  const next = new Date(user.displayNameChangedAt.getTime() + COOLDOWN_MS)
  return next > now ? next : null
}

export function renameCooldownStart(now = new Date()): Date {
  return new Date(now.getTime() - COOLDOWN_MS)
}
