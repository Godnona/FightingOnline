import type { FastifyInstance } from 'fastify'

import { adminRoutes } from './routes/admin'
import { authRoutes } from './routes/auth'
import { health } from './routes/health'
import { profileRoutes } from './routes/profile'

export async function registerRoutes(app: FastifyInstance) {
  await app.register(health)
  await app.register(authRoutes, { prefix: '/auth' })
  await app.register(profileRoutes)
  await app.register(adminRoutes, { prefix: '/admin' })
}
