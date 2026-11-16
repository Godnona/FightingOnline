import type { IdentityProvider, UserIdentity } from '@/generated/prisma/client'

import type { CreateIdentityData } from './users-repository'

export type UpdateIdentityData = Partial<
  Pick<UserIdentity, 'email' | 'emailVerifiedAt' | 'passwordHash' | 'lastUsedAt'>
>

export type DeleteIdentityResult = 'deleted' | 'not_found' | 'last_identity'

export interface UserIdentitiesRepository {
  findById(id: string): Promise<UserIdentity | null>

  findByProvider(provider: IdentityProvider, providerUserId: string): Promise<UserIdentity | null>

  findManyByEmail(email: string): Promise<UserIdentity[]>

  findManyByUserId(userId: string): Promise<UserIdentity[]>

  create(userId: string, data: CreateIdentityData): Promise<UserIdentity>

  update(id: string, data: UpdateIdentityData): Promise<UserIdentity>

  delete(id: string): Promise<void>

  /** atomic: two concurrent calls can't remove the last two identities */
  deleteUnlessLast(userId: string, provider: IdentityProvider): Promise<DeleteIdentityResult>
}
