import { z } from 'zod'

import { userResponseSchema } from './auth-schemas'

export const avatarIdSchema = z.string().trim().min(1).max(64)

export const profileResponseSchema = userResponseSchema.extend({
  createdAt: z.iso.datetime(),
  /** null when the player can rename now */
  nextDisplayNameChangeAt: z.iso.datetime().nullable(),
})

export const publicProfileResponseSchema = z.object({
  id: z.uuid(),
  displayName: z.string(),
  avatar: z.string().nullable(),
})
