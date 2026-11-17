import type { IdentityProvider, PrismaClient } from '@/generated/prisma/client'
import type {
  DeleteIdentityResult,
  UpdateIdentityData,
  UserIdentitiesRepository,
} from '@/repositories/user-identities-repository'
import type { CreateIdentityData } from '@/repositories/users-repository'
import { normalizeEmail } from '@/utils/email'

import { assertIdentityInvariants, identityPersistenceData } from './identity-invariants'
import { withUniqueConstraintTranslation } from './prisma-errors'
import { withTransactionRetry } from './transaction-retry'

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
    return this.db.userIdentity.findMany({
      where: { emailNormalized: normalizeEmail(email) },
      orderBy: { createdAt: 'asc' },
    })
  }

  findManyByUserId(userId: string) {
    return this.db.userIdentity.findMany({ where: { userId }, orderBy: { createdAt: 'asc' } })
  }

  create(userId: string, data: CreateIdentityData) {
    return withUniqueConstraintTranslation(() =>
      this.db.userIdentity.create({ data: { ...identityPersistenceData(data), userId } }),
    )
  }

  async update(id: string, data: UpdateIdentityData) {
    const current = await this.db.userIdentity.findUniqueOrThrow({ where: { id } })
    assertIdentityInvariants({
      provider: current.provider,
      email: data.email === undefined ? current.email : data.email,
      passwordHash: data.passwordHash === undefined ? current.passwordHash : data.passwordHash,
    })

    return this.db.userIdentity.update({
      where: { id },
      data: {
        ...data,
        ...(data.email === undefined
          ? {}
          : { emailNormalized: data.email ? normalizeEmail(data.email) : null }),
      },
    })
  }

  async delete(id: string) {
    await this.db.userIdentity.delete({ where: { id } })
  }

  deleteUnlessLast(userId: string, provider: IdentityProvider): Promise<DeleteIdentityResult> {
    return withTransactionRetry(() =>
      this.db.$transaction(async (tx) => {
        // Writing the parent document serializes concurrent unlink transactions in MongoDB.
        await tx.user.update({ where: { id: userId }, data: { updatedAt: new Date() } })

        const identities = await tx.userIdentity.findMany({
          where: { userId },
          select: { id: true, provider: true },
        })
        const target = identities.find((identity) => identity.provider === provider)

        if (!target) return 'not_found'
        if (identities.length === 1) return 'last_identity'

        await tx.userIdentity.delete({ where: { id: target.id } })
        return 'deleted'
      }),
    )
  }
}
