import type { IdentityProvider, PrismaClient } from '@/generated/prisma/client'
import type {
  DeleteIdentityResult,
  UpdateIdentityData,
  UserIdentitiesRepository,
} from '@/repositories/user-identities-repository'
import type { CreateIdentityData } from '@/repositories/users-repository'

import { withUniqueConstraintTranslation } from './prisma-errors'

export class PrismaUserIdentitiesRepository implements UserIdentitiesRepository {
  constructor(private readonly db: PrismaClient) {}

  findById(id: string) {
    return this.db.userIdentity.findUnique({ where: { id } })
  }

  findByProvider(provider: IdentityProvider, providerUserId: string) {
    return this.db.userIdentity.findUnique({
      where: { provider_providerUserId: { provider, providerUserId } },
    })
  }

  findManyByEmail(email: string) {
    return this.db.userIdentity.findMany({ where: { email }, orderBy: { createdAt: 'asc' } })
  }

  findManyByUserId(userId: string) {
    return this.db.userIdentity.findMany({ where: { userId }, orderBy: { createdAt: 'asc' } })
  }

  create(userId: string, data: CreateIdentityData) {
    return withUniqueConstraintTranslation(() =>
      this.db.userIdentity.create({ data: { ...data, userId } }),
    )
  }

  update(id: string, data: UpdateIdentityData) {
    return this.db.userIdentity.update({ where: { id }, data })
  }

  async delete(id: string) {
    await this.db.userIdentity.delete({ where: { id } })
  }

  deleteUnlessLast(userId: string, provider: IdentityProvider): Promise<DeleteIdentityResult> {
    return this.db.$transaction(async (tx) => {
      // lock the user row so concurrent unlinks run one after the other
      await tx.$queryRaw`SELECT id FROM users WHERE id = ${userId}::uuid FOR UPDATE`

      const identities = await tx.userIdentity.findMany({
        where: { userId },
        select: { id: true, provider: true },
      })
      const target = identities.find((identity) => identity.provider === provider)

      if (!target) return 'not_found'
      if (identities.length === 1) return 'last_identity'

      await tx.userIdentity.delete({ where: { id: target.id } })
      return 'deleted'
    })
  }
}
