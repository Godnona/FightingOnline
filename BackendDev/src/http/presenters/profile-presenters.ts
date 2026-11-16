import type { User } from '@/generated/prisma/client'
import { nextDisplayNameChangeAt } from '@/use-cases/profile/display-name-policy'

import { presentUser } from './auth-presenters'

export function presentProfile(user: User) {
  return {
    ...presentUser(user),
    createdAt: user.createdAt.toISOString(),
    nextDisplayNameChangeAt: nextDisplayNameChangeAt(user)?.toISOString() ?? null,
  }
}

export function presentPublicProfile(user: User) {
  return { id: user.id, displayName: user.displayName, avatar: user.avatar }
}
