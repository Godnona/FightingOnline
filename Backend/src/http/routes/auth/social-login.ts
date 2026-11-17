import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { socialLoginController } from '@/http/controllers/auth/social-login-controller'
import {
  appleProfileSchema,
  deviceInfoSchema,
  providerTokenSchema,
  socialAuthResponseSchema,
} from '@/http/schemas/auth-schemas'
import { authRateLimit } from '@/http/utils/rate-limits'
import type { SocialProvider } from '@/lib/oauth/social-token-verifier'

export const socialLoginBodySchema = z.object({
  token: providerTokenSchema,
  deviceInfo: deviceInfoSchema,
  /** Apple supplies the name only on the first authorization. Ignored for other providers. */
  appleProfile: appleProfileSchema.optional(),
})

export type SocialLoginBody = z.infer<typeof socialLoginBodySchema>

const routes: ReadonlyArray<{ path: string; provider: SocialProvider; summary: string }> = [
  { path: '/google', provider: 'GOOGLE', summary: 'Sign in with a Google ID token' },
  {
    path: '/facebook',
    provider: 'FACEBOOK',
    summary: 'Sign in with a Facebook access token or Limited Login token',
  },
  { path: '/apple', provider: 'APPLE', summary: 'Sign in with an Apple identity token' },
]

export async function socialLogin(app: FastifyInstance) {
  for (const { path, provider, summary } of routes) {
    app.withTypeProvider<ZodTypeProvider>().post(
      path,
      {
        config: { rateLimit: authRateLimit },
        schema: {
          tags: ['auth'],
          summary,
          description:
            'Creates the account on the first sign-in. Fails with EMAIL_ALREADY_IN_USE when the email belongs to another account: sign in to it and link this provider instead.',
          body: socialLoginBodySchema,
          response: { 200: socialAuthResponseSchema },
        },
      },
      socialLoginController(provider),
    )
  }
}
