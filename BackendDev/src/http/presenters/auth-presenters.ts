import type { User, UserIdentity } from '@/generated/prisma/client'
import type { AuthSession } from '@/use-cases/auth/session-issuer'

export function presentUser(user: User) {
  return {
    id: user.id,
    displayName: user.displayName,
    avatar: user.avatar,
    role: user.role,
  }
}

export function presentIdentity(identity: UserIdentity) {
  return {
    provider: identity.provider,
    email: identity.email,
    linkedAt: identity.createdAt.toISOString(),
    lastUsedAt: identity.lastUsedAt?.toISOString() ?? null,
  }
}

export function presentSession(session: AuthSession) {
  return {
    tokenType: 'Bearer' as const,
    accessToken: session.accessToken,
    accessTokenExpiresAt: session.accessTokenExpiresAt.toISOString(),
    refreshToken: session.refreshToken,
    refreshTokenExpiresAt: session.refreshTokenExpiresAt.toISOString(),
  }
}
