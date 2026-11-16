import { z } from 'zod'

import { DISPLAY_NAME_PATTERN } from '@/utils/display-name'
import { isCommonPassword, PASSWORD_MAX_LENGTH, PASSWORD_MIN_LENGTH } from '@/utils/password-policy'
import { RESET_CODE_LENGTH } from '@/utils/secure-token'

export const emailSchema = z.string().trim().max(254).pipe(z.email('Invalid email address'))

export const newPasswordSchema = z
  .string()
  .min(PASSWORD_MIN_LENGTH, `Password must have at least ${PASSWORD_MIN_LENGTH} characters`)
  .max(PASSWORD_MAX_LENGTH, `Password must have at most ${PASSWORD_MAX_LENGTH} characters`)
  .refine((password) => !isCommonPassword(password), 'This password is too common')

export const currentPasswordSchema = z.string().min(1).max(PASSWORD_MAX_LENGTH)

export const displayNameSchema = z
  .string()
  .trim()
  .regex(
    DISPLAY_NAME_PATTERN,
    'Display name must have 3 to 16 characters: letters, digits and underscore only',
  )

export const deviceInfoSchema = z.string().trim().min(1).max(200).optional()

export const opaqueTokenSchema = z.string().min(1).max(512)

// players may type "123 456" or "123-456"
export const resetCodeSchema = z
  .string()
  .max(20)
  .transform((value) => value.replace(/[\s-]/g, ''))
  .pipe(z.string().regex(new RegExp(`^\\d{${RESET_CODE_LENGTH}}$`), 'The code has 6 digits'))

export const userResponseSchema = z.object({
  id: z.uuid(),
  displayName: z.string(),
  avatar: z.string().nullable(),
  role: z.enum(['PLAYER', 'ADMIN']),
})

export const sessionResponseSchema = z.object({
  tokenType: z.literal('Bearer'),
  accessToken: z.string(),
  accessTokenExpiresAt: z.iso.datetime(),
  refreshToken: z.string(),
  refreshTokenExpiresAt: z.iso.datetime(),
})

export const authResponseSchema = z.object({
  user: userResponseSchema,
  session: sessionResponseSchema,
})

export const providerTokenSchema = z.string().trim().min(1).max(8192)

export const socialAuthResponseSchema = authResponseSchema.extend({
  isNewUser: z.boolean(),
})

export const socialProviderParamSchema = z.enum(['google', 'facebook', 'apple'])

export const identityProviderParamSchema = z.enum(['email', 'google', 'facebook', 'apple'])

export const identityResponseSchema = z.object({
  provider: z.enum(['EMAIL', 'GOOGLE', 'FACEBOOK', 'APPLE', 'STEAM', 'GAMECENTER', 'PLAYGAMES']),
  email: z.string().nullable(),
  linkedAt: z.iso.datetime(),
  lastUsedAt: z.iso.datetime().nullable(),
})
