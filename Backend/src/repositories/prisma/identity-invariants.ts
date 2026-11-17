import type { IdentityProvider } from '@/generated/prisma/client'
import type { CreateIdentityData } from '@/repositories/users-repository'
import { normalizeEmail } from '@/utils/email'

type IdentityValues = Pick<CreateIdentityData, 'email' | 'passwordHash'> & {
  provider: IdentityProvider
}

export function assertIdentityInvariants(identity: IdentityValues): void {
  if (identity.provider === 'EMAIL' && !identity.email) {
    throw new Error('EMAIL identities must have an email address')
  }

  if (identity.provider !== 'EMAIL' && identity.passwordHash != null) {
    throw new Error('Only EMAIL identities may have a password hash')
  }
}

export function identityPersistenceData(identity: CreateIdentityData) {
  assertIdentityInvariants(identity)
  return {
    ...identity,
    emailNormalized: identity.email ? normalizeEmail(identity.email) : null,
  }
}
