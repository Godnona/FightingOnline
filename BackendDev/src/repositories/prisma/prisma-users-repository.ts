import type { PrismaClient } from '@/generated/prisma/client'
import type {
  CreateUserData,
  UpdateUserData,
  UsersRepository,
  UserWithIdentities,
} from '@/repositories/users-repository'

import { withUniqueConstraintTranslation } from './prisma-errors'

export class PrismaUsersRepository implements UsersRepository {
  constructor(private readonly db: PrismaClient) {}

  findById(id: string) {
    return this.db.user.findUnique({ where: { id } })
  }

  findByDisplayName(displayName: string) {
    return this.db.user.findUnique({ where: { displayName } })
  }

  create({ identity, ...user }: CreateUserData): Promise<UserWithIdentities> {
    return withUniqueConstraintTranslation(() =>
      this.db.user.create({
        data: { ...user, identities: { create: identity } },
        include: { identities: true },
      }),
    )
  }

  update(id: string, data: UpdateUserData) {
    return this.db.user.update({ where: { id }, data })
  }

  async rename(id: string, displayName: string, lastChangeBefore: Date) {
    const { count } = await withUniqueConstraintTranslation(() =>
      this.db.user.updateMany({
        where: {
          id,
          OR: [{ displayNameChangedAt: null }, { displayNameChangedAt: { lte: lastChangeBefore } }],
        },
        data: { displayName, displayNameChangedAt: new Date() },
      }),
    )
    return count > 0 ? this.findById(id) : null
  }
}
