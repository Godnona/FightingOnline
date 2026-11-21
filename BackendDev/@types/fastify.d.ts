import 'fastify'

import type { User } from '@/generated/prisma/client'

declare module 'fastify' {
  interface FastifyRequest {
    authUser: User | null
    getCurrentUser(): User
    getCurrentUserId(): string
  }
}
