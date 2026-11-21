import {
  AbilityBuilder,
  createMongoAbility,
  type ForcedSubject,
  type MongoAbility,
} from '@casl/ability'

import type { Role, User } from '@/generated/prisma/client'

export type Action = 'manage' | 'create' | 'read' | 'update' | 'delete'

type UserSubject = ForcedSubject<'User'> & Pick<User, 'id'>

// grows with each module: Inventory, Match, Sanction...
export type Subject = 'all' | 'Role' | 'User' | UserSubject

export type AppAbility = MongoAbility<[Action, Subject]>

export function defineAbilityFor(user: Pick<User, 'id' | 'role'>): AppAbility {
  const { can, build } = new AbilityBuilder<AppAbility>(createMongoAbility)

  const rules: Record<Role, () => void> = {
    PLAYER: () => {
      can('read', 'User')
      can('update', 'User', { id: user.id })
    },
    ADMIN: () => {
      can('manage', 'all')
    },
  }
  rules[user.role]()

  return build()
}
