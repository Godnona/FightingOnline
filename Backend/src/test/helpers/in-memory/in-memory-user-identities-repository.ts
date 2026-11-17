import { randomUUID } from 'node:crypto'

import type { IdentityProvider, UserIdentity } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type {
  UpdateIdentityData,
  UserIdentitiesRepository,
} from '@/repositories/user-identities-repository'
import type { CreateIdentityData } from '@/repositories/users-repository'

import { equalsIgnoringCase, type InMemoryDatabase } from './in-memory-database'

const byCreation = (a: UserIdentity, b: UserIdentity) =>
  a.createdAt.getTime() - b.createdAt.getTime()

export class InMemoryUserIdentitiesRepository implements UserIdentitiesRepository {
  constructor(private readonly db: InMemoryDatabase) {}

  buildRow(userId: string, data: CreateIdentityData): UserIdentity {
    if (data.provider !== 'EMAIL' && data.passwordHash) {
      throw new Error('check constraint violated: password only for EMAIL identities')
    }
    if (data.provider === 'EMAIL' && !data.email) {
      throw new Error('check constraint violated: EMAIL identities require an email')
    }

    const identities = this.db.identities
    if (
      identities.some(
        (identity) =>
          identity.provider === data.provider && identity.providerUserId === data.providerUserId,
      )
    ) {
      throw new UniqueConstraintError(['provider', 'providerUserId'])
    }
    if (
      identities.some(
        (identity) => identity.userId === userId && identity.provider === data.provider,
      )
    ) {
      throw new UniqueConstraintError(['userId', 'provider'])
    }

    const now = new Date()
    return {
      id: randomUUID(),
      userId,
      provider: data.provider,
      providerUserId: data.providerUserId,
      email: data.email ?? null,
      emailNormalized: data.email?.trim().toLowerCase() ?? null,
      emailVerifiedAt: data.emailVerifiedAt ?? null,
      passwordHash: data.passwordHash ?? null,
      lastUsedAt: null,
      createdAt: now,
      updatedAt: now,
    }
  }

  async findById(id: string) {
    const identity = this.db.identities.find((item) => item.id === id)
    return identity ? { ...identity } : null
  }

  async findByProvider(provider: IdentityProvider, providerUserId: string) {
    const identity = this.db.identities.find(
      (item) => item.provider === provider && item.providerUserId === providerUserId,
    )
    return identity ? { ...identity } : null
  }

  async findManyByEmail(email: string) {
    return this.db.identities
      .filter((item) => equalsIgnoringCase(item.email, email))
      .sort(byCreation)
      .map((item) => ({ ...item }))
  }

  async findManyByUserId(userId: string) {
    return this.db.identities
      .filter((item) => item.userId === userId)
      .sort(byCreation)
      .map((item) => ({ ...item }))
  }

  async create(userId: string, data: CreateIdentityData) {
    if (!this.db.users.some((user) => user.id === userId)) {
      throw new Error('foreign key violated: user does not exist')
    }
    const row = this.buildRow(userId, data)
    this.db.identities.push(row)
    return { ...row }
  }

  async update(id: string, data: UpdateIdentityData) {
    const identity = this.db.identities.find((item) => item.id === id)
    if (!identity) throw new Error(`identity ${id} not found`)
    Object.assign(identity, data, {
      ...(data.email === undefined
        ? {}
        : { emailNormalized: data.email?.trim().toLowerCase() ?? null }),
      updatedAt: new Date(),
    })
    return { ...identity }
  }

  async delete(id: string) {
    const index = this.db.identities.findIndex((item) => item.id === id)
    if (index === -1) throw new Error(`identity ${id} not found`)
    this.db.identities.splice(index, 1)
  }

  async deleteUnlessLast(userId: string, provider: IdentityProvider) {
    const owned = this.db.identities.filter((item) => item.userId === userId)
    const target = owned.find((item) => item.provider === provider)

    if (!target) return 'not_found' as const
    if (owned.length === 1) return 'last_identity' as const

    await this.delete(target.id)
    return 'deleted' as const
  }
}
