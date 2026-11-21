import { z } from 'zod'

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const

export type LogLevel = (typeof logLevels)[number]

const commaSeparatedList = z
  .string()
  .default('')
  .transform((value) =>
    value
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean),
  )

const optionalString = z
  .string()
  .trim()
  .optional()
  .transform((value) => value || undefined)

export const envSchema = z
  .object({
    NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
    HOST: z.string().min(1).default('0.0.0.0'),
    PORT: z.coerce.number().int().min(1).max(65535).default(3333),
    LOG_LEVEL: z.enum(logLevels).optional(),
    TRUST_PROXY: z.stringbool().default(false),
    CORS_ORIGINS: commaSeparatedList,
    DATABASE_URL: z.url({ protocol: /^postgres(ql)?$/ }),

    JWT_SECRET: z.string().min(32, 'must be at least 32 characters long'),
    JWT_ISSUER: z.string().min(1).default('ripptopia-api'),
    JWT_AUDIENCE: z.string().min(1).default('ripptopia-client'),
    ACCESS_TOKEN_TTL_SECONDS: z.coerce
      .number()
      .int()
      .positive()
      .default(15 * 60),
    REFRESH_TOKEN_TTL_DAYS: z.coerce.number().int().positive().default(30),
    PASSWORD_RESET_CODE_TTL_MINUTES: z.coerce.number().int().positive().default(30),
    AUTH_RATE_LIMIT_MAX: z.coerce.number().int().positive().default(10),
    AUTH_RATE_LIMIT_WINDOW_SECONDS: z.coerce.number().int().positive().default(60),

    MAIL_TRANSPORT: z.enum(['log', 'memory']).default('log'),
    MAIL_FROM: z.string().min(1).default('Ripptopia <no-reply@ripptopia.local>'),

    GOOGLE_CLIENT_IDS: commaSeparatedList,
    FACEBOOK_APP_ID: optionalString,
    FACEBOOK_APP_SECRET: optionalString,
    FACEBOOK_GRAPH_API_VERSION: z
      .string()
      .regex(/^v\d+\.\d+$/, 'must look like v24.0')
      .default('v24.0'),
    APPLE_CLIENT_IDS: commaSeparatedList,
    SOCIAL_AUTH_MOCK: z.stringbool().default(false),

    // placeholder ids until the game sends the real list
    AVATAR_IDS: commaSeparatedList
      .pipe(z.array(z.string().regex(/^[\w-]{1,64}$/, 'avatar ids: letters, digits, _ and -')))
      .transform((ids) =>
        ids.length > 0
          ? ids
          : Array.from({ length: 8 }, (_, i) => `avatar_${String(i + 1).padStart(2, '0')}`),
      ),
  })
  .refine((env) => Boolean(env.FACEBOOK_APP_ID) === Boolean(env.FACEBOOK_APP_SECRET), {
    path: ['FACEBOOK_APP_SECRET'],
    message: 'FACEBOOK_APP_ID and FACEBOOK_APP_SECRET must be set together',
  })

export type Env = Omit<z.infer<typeof envSchema>, 'LOG_LEVEL'> & { LOG_LEVEL: LogLevel }

export function parseEnv(source: Record<string, string | undefined>): Env {
  const result = envSchema.safeParse(source)

  if (!result.success) {
    throw new Error(`Invalid environment variables:\n${z.prettifyError(result.error)}`)
  }

  const data = result.data

  return {
    ...data,
    LOG_LEVEL: data.LOG_LEVEL ?? (data.NODE_ENV === 'test' ? 'silent' : 'info'),
  }
}

export const env = parseEnv(process.env)
