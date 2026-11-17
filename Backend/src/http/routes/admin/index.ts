import type { FastifyInstance } from 'fastify'

import { setUserRole } from './set-user-role'

export async function adminRoutes(app: FastifyInstance) {
  await app.register(setUserRole)
}
