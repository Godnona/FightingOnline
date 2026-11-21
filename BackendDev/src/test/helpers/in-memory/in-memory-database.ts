import type {
  PasswordResetToken,
  RefreshToken,
  User,
  UserIdentity,
} from '@/generated/prisma/client'

export class InMemoryDatabase {
  users: User[] = []
  identities: UserIdentity[] = []
  refreshTokens: RefreshToken[] = []
  passwordResetTokens: PasswordResetToken[] = []
}

export const equalsIgnoringCase = (a: string | null | undefined, b: string) =>
  a?.toLowerCase() === b.toLowerCase()
