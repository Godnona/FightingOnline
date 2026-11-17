import { randomUUID } from 'node:crypto'

import type { User } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'
import type {
  CreateUserData,
  UpdateUserData,
  UsersRepository,
  UserWithIdentities,
} from '@/repositories/users-repository'

import { equalsIgnoringCase, type InMemoryDatabase } from './in-memory-database'
import { InMemoryUserIdentitiesRepository } from './in-memory-user-identities-repository'

export class InMemoryUsersRepository implements UsersRepository {
  private readonly identities: InMemoryUserIdentitiesRepository

  constructor(private readonly db: InMemoryDatabase) {
    this.identities = new InMemoryUserIdentitiesRepository(db)
  }

  async findById(id: string) {
    const user = this.db.users.find((item) => item.id === id)
    return user ? { ...user } : null
  }

  async findByDisplayName(displayName: string) {
    const user = this.db.users.find((item) => equalsIgnoringCase(item.displayName, displayName))
    return user ? { ...user } : null
  }

  async create({ identity, ...data }: CreateUserData): Promise<UserWithIdentities> {
    if (this.db.users.some((item) => equalsIgnoringCase(item.displayName, data.displayName))) {
      throw new UniqueConstraintError(['displayName'])
    }

    const now = new Date()
    const user: User = {
      id: randomUUID(),
      displayName: data.displayName,
      displayNameNormalized: data.displayName.toLowerCase(),
      avatar: data.avatar ?? null,
      role: data.role ?? 'PLAYER',
      status: 'ACTIVE',
      displayNameChangedAt: null,
      createdAt: now,
      updatedAt: now,
    }

    const identityRow = this.identities.buildRow(user.id, identity)

    this.db.users.push(user)
    this.db.identities.push(identityRow)

    return { ...user, identities: [{ ...identityRow }] }
  }

  async update(id: string, data: UpdateUserData) {
    const user = this.db.users.find((item) => item.id === id)
    if (!user) throw new Error(`user ${id} not found`)
    Object.assign(user, data, { updatedAt: new Date() })
    return { ...user }
  }

  async rename(id: string, displayName: string, lastChangeBefore: Date) {
    const user = this.db.users.find((item) => item.id === id)
    if (!user) return null
    if (user.displayNameChangedAt && user.displayNameChangedAt > lastChangeBefore) return null

    const taken = this.db.users.some(
      (item) => item.id !== id && equalsIgnoringCase(item.displayName, displayName),
    )
    if (taken) throw new UniqueConstraintError(['displayName'])

    const now = new Date()
    Object.assign(user, {
      displayName,
      displayNameNormalized: displayName.toLowerCase(),
      displayNameChangedAt: now,
      updatedAt: now,
    })
    return { ...user }
  }
}
