import { createHmac } from 'node:crypto'

import { normalizeEmail } from '@/utils/email'

import {
  invalidToken,
  type SocialTokenVerification,
  type SocialTokenVerifier,
} from './social-token-verifier'

export type FacebookGraphVerifierOptions = {
  appId: string
  appSecret: string
  apiVersion: string
  fetch?: typeof fetch
  timeoutMs?: number
}

type GraphResponse = { ok: true; body: unknown } | { ok: false; reason: 'invalid' | 'unavailable' }

const GRAPH_URL = 'https://graph.facebook.com'

function readString(value: unknown, key: string): string | undefined {
  if (typeof value !== 'object' || value === null) return undefined
  const field = (value as Record<string, unknown>)[key]
  return typeof field === 'string' ? field : undefined
}

/** classic facebook access tokens, checked with debug_token and then /me */
export class FacebookGraphVerifier implements SocialTokenVerifier {
  private readonly fetch: typeof fetch

  constructor(private readonly options: FacebookGraphVerifierOptions) {
    this.fetch = options.fetch ?? globalThis.fetch
  }

  async verify(token: string): Promise<SocialTokenVerification> {
    const { appId, appSecret } = this.options

    const debug = await this.get('debug_token', {
      input_token: token,
      access_token: `${appId}|${appSecret}`,
    })
    if (!debug.ok) return { valid: false, reason: debug.reason }

    const data = (debug.body as { data?: Record<string, unknown> }).data
    const userId = readString(data, 'user_id')
    if (data?.is_valid !== true || readString(data, 'app_id') !== appId || !userId) {
      return invalidToken
    }

    const me = await this.get('me', {
      fields: 'id,email',
      access_token: token,
      appsecret_proof: createHmac('sha256', appSecret).update(token).digest('hex'),
    })
    if (!me.ok) return { valid: false, reason: me.reason }
    if (readString(me.body, 'id') !== userId) return invalidToken

    // the graph api only returns confirmed emails
    const email = readString(me.body, 'email')
    return {
      valid: true,
      profile: { providerUserId: userId, email: email ? normalizeEmail(email) : null },
    }
  }

  private async get(path: string, params: Record<string, string>): Promise<GraphResponse> {
    const query = new URLSearchParams(params).toString()
    const url = `${GRAPH_URL}/${this.options.apiVersion}/${path}?${query}`
    try {
      const response = await this.fetch(url, {
        signal: AbortSignal.timeout(this.options.timeoutMs ?? 5000),
      })
      if (response.status >= 500) return { ok: false, reason: 'unavailable' }
      // graph answers 4xx for malformed, expired or foreign tokens
      if (!response.ok) return { ok: false, reason: 'invalid' }
      return { ok: true, body: await response.json() }
    } catch {
      return { ok: false, reason: 'unavailable' }
    }
  }
}

const JWT_SHAPE = /^[\w-]+\.[\w-]+\.[\w-]+$/

/** ios limited login sends an oidc token, everything else sends a classic access token */
export class FacebookTokenVerifier implements SocialTokenVerifier {
  constructor(
    private readonly limitedLogin: SocialTokenVerifier,
    private readonly graph: SocialTokenVerifier,
  ) {}

  verify(token: string) {
    return JWT_SHAPE.test(token) ? this.limitedLogin.verify(token) : this.graph.verify(token)
  }
}
