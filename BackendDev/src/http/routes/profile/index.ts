import type { FastifyInstance } from 'fastify'

import { getPlayerProfile } from './get-player-profile'
import { getProfile } from './get-profile'
import { updateProfile } from './update-profile'

export async function profileRoutes(app: FastifyInstance) {
  await app.register(getProfile)
  await app.register(updateProfile)
  await app.register(getPlayerProfile)
}
