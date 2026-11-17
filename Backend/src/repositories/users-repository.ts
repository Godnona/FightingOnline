import type { IdentityProvider, Role, User, UserIdentity } from '@/generated/prisma/client'

export type CreateIdentityData = {
  provider: IdentityProvider
  providerUserId: string
  email?: string | null
  emailVerifiedAt?: Date | null
  providerGivenName?: string | null
  providerFamilyName?: string | null
  passwordHash?: string | null
}

export type CreateUserData = {
  displayName: string
  avatar?: string | null
  role?: Role
  identity: CreateIdentityData
}

export type UserWithIdentities = User & { identities: UserIdentity[] }

export type UpdateUserData = Partial<Pick<User, 'avatar' | 'role'>>

export interface UsersRepository {
  findById(id: string): Promise<User | null>

  findByDisplayName(displayName: string): Promise<User | null>

  create(data: CreateUserData): Promise<UserWithIdentities>

  update(id: string, data: UpdateUserData): Promise<User>

  /**
   * Atomic: renames only if the player never renamed or did it before `lastChangeBefore`.
   * Returns null when the cooldown is still running.
   */
  rename(id: string, displayName: string, lastChangeBefore: Date): Promise<User | null>
}
