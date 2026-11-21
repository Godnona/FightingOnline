import { subject } from '@casl/ability'
import { describe, expect, it } from 'vitest'

import { defineAbilityFor } from '@/lib/casl/define-ability'

const PLAYER_ID = 'player-1'

describe('defineAbilityFor', () => {
  it('lets a player read users and update only their own', () => {
    const ability = defineAbilityFor({ id: PLAYER_ID, role: 'PLAYER' })

    expect(ability.can('read', 'User')).toBe(true)
    expect(ability.can('update', subject('User', { id: PLAYER_ID }))).toBe(true)
    expect(ability.can('update', subject('User', { id: 'someone-else' }))).toBe(false)
    expect(ability.can('delete', 'User')).toBe(false)
    expect(ability.can('update', 'Role')).toBe(false)
  })

  it('lets an admin do everything', () => {
    const ability = defineAbilityFor({ id: 'admin-1', role: 'ADMIN' })

    expect(ability.can('update', 'Role')).toBe(true)
    expect(ability.can('delete', subject('User', { id: PLAYER_ID }))).toBe(true)
    expect(ability.can('manage', 'all')).toBe(true)
  })
})
