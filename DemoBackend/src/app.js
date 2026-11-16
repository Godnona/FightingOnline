import {
  createHmac,
  randomBytes,
  randomUUID,
  timingSafeEqual,
} from 'node:crypto'

const DEFAULT_ACCOUNTS = [
  {
    id: '11111111-1111-4111-8111-111111111111',
    email: 'player1@ripptopia.demo',
    password: 'Demo123!',
    displayName: 'PlayerOne',
    avatar: null,
    role: 'PLAYER',
  },
  {
    id: '22222222-2222-4222-8222-222222222222',
    email: 'player2@ripptopia.demo',
    password: 'Demo123!',
    displayName: 'PlayerTwo',
    avatar: null,
    role: 'PLAYER',
  },
]

const ACCESS_TOKEN_TTL_SECONDS = 15 * 60
const REFRESH_TOKEN_TTL_SECONDS = 7 * 24 * 60 * 60
const MAX_BODY_BYTES = 16 * 1024
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function encodeJson(value) {
  return Buffer.from(JSON.stringify(value)).toString('base64url')
}

function safeEqual(left, right) {
  const leftBuffer = Buffer.from(String(left))
  const rightBuffer = Buffer.from(String(right))
  if (leftBuffer.length !== rightBuffer.length) return false
  return timingSafeEqual(leftBuffer, rightBuffer)
}

function parseAccounts(rawValue) {
  if (!rawValue) return DEFAULT_ACCOUNTS.map((account) => ({ ...account }))

  let accounts
  try {
    accounts = JSON.parse(rawValue)
  } catch {
    throw new Error('DEMO_ACCOUNTS_JSON must be valid JSON')
  }

  if (!Array.isArray(accounts) || accounts.length === 0) {
    throw new Error('DEMO_ACCOUNTS_JSON must be a non-empty JSON array')
  }

  return accounts.map((account, index) => {
    if (
      !account ||
      typeof account.id !== 'string' ||
      typeof account.email !== 'string' ||
      typeof account.password !== 'string' ||
      typeof account.displayName !== 'string'
    ) {
      throw new Error(`Demo account at index ${index} is missing a required string field`)
    }

    return {
      id: account.id,
      email: account.email.trim().toLowerCase(),
      password: account.password,
      displayName: account.displayName,
      avatar: typeof account.avatar === 'string' ? account.avatar : null,
      role: account.role === 'ADMIN' ? 'ADMIN' : 'PLAYER',
    }
  })
}

function parseAllowedOrigins(rawValue) {
  return new Set(
    String(rawValue || '*')
      .split(',')
      .map((origin) => origin.trim())
      .filter(Boolean),
  )
}

function bearerToken(request) {
  const authorization = request.headers.authorization || ''
  const match = /^Bearer\s+(.+)$/i.exec(authorization)
  return match?.[1] || null
}

function publicUser(account) {
  return {
    id: account.id,
    displayName: account.displayName,
    avatar: account.avatar,
    role: account.role,
  }
}

export function createApp(options = {}) {
  const secret = options.secret || process.env.JWT_SECRET || 'local-demo-secret-change-before-deploy'
  const accounts = parseAccounts(options.accountsJson ?? process.env.DEMO_ACCOUNTS_JSON)
  const allowedOrigins = parseAllowedOrigins(options.corsOrigins ?? process.env.CORS_ORIGINS)
  const refreshSessions = new Map()
  const failedLogins = new Map()
  const startedAt = new Date().toISOString()

  function signAccessToken(account, nowSeconds = Math.floor(Date.now() / 1000)) {
    const header = encodeJson({ alg: 'HS256', typ: 'JWT' })
    const payload = encodeJson({
      sub: account.id,
      role: account.role,
      iss: 'ripptopia-demo-backend',
      aud: 'ripptopia-client',
      iat: nowSeconds,
      exp: nowSeconds + ACCESS_TOKEN_TTL_SECONDS,
    })
    const unsignedToken = `${header}.${payload}`
    const signature = createHmac('sha256', secret).update(unsignedToken).digest('base64url')
    return `${unsignedToken}.${signature}`
  }

  function verifyAccessToken(token) {
    if (!token) return { error: 'AUTHENTICATION_REQUIRED' }
    const parts = token.split('.')
    if (parts.length !== 3) return { error: 'INVALID_ACCESS_TOKEN' }

    const unsignedToken = `${parts[0]}.${parts[1]}`
    const expected = createHmac('sha256', secret).update(unsignedToken).digest('base64url')
    if (!safeEqual(parts[2], expected)) return { error: 'INVALID_ACCESS_TOKEN' }

    try {
      const payload = JSON.parse(Buffer.from(parts[1], 'base64url').toString('utf8'))
      if (!payload.exp || payload.exp <= Math.floor(Date.now() / 1000)) {
        return { error: 'ACCESS_TOKEN_EXPIRED' }
      }
      const account = accounts.find((candidate) => candidate.id === payload.sub)
      return account ? { account } : { error: 'INVALID_ACCESS_TOKEN' }
    } catch {
      return { error: 'INVALID_ACCESS_TOKEN' }
    }
  }

  function issueSession(account) {
    const now = Date.now()
    const accessTokenExpiresAt = new Date(now + ACCESS_TOKEN_TTL_SECONDS * 1000)
    const refreshTokenExpiresAt = new Date(now + REFRESH_TOKEN_TTL_SECONDS * 1000)
    const refreshToken = randomBytes(48).toString('base64url')
    refreshSessions.set(refreshToken, {
      userId: account.id,
      expiresAt: refreshTokenExpiresAt.getTime(),
    })

    return {
      tokenType: 'Bearer',
      accessToken: signAccessToken(account),
      accessTokenExpiresAt: accessTokenExpiresAt.toISOString(),
      refreshToken,
      refreshTokenExpiresAt: refreshTokenExpiresAt.toISOString(),
    }
  }

  function responseHeaders(request, requestId) {
    const headers = {
      'cache-control': 'no-store',
      'content-type': 'application/json; charset=utf-8',
      'x-content-type-options': 'nosniff',
      'x-request-id': requestId,
    }
    const origin = request.headers.origin
    if (allowedOrigins.has('*')) headers['access-control-allow-origin'] = '*'
    else if (origin && allowedOrigins.has(origin)) {
      headers['access-control-allow-origin'] = origin
      headers.vary = 'Origin'
    }
    return headers
  }

  function send(request, response, status, body, requestId, extraHeaders = {}) {
    response.writeHead(status, { ...responseHeaders(request, requestId), ...extraHeaders })
    response.end(JSON.stringify(body))
  }

  function sendError(request, response, status, code, message, requestId, extraHeaders) {
    send(request, response, status, { code, message }, requestId, extraHeaders)
  }

  async function readJson(request) {
    let size = 0
    const chunks = []
    for await (const chunk of request) {
      size += chunk.length
      if (size > MAX_BODY_BYTES) {
        const error = new Error('Request body is too large')
        error.code = 'BODY_TOO_LARGE'
        throw error
      }
      chunks.push(chunk)
    }
    const body = Buffer.concat(chunks).toString('utf8')
    if (!body) return {}
    return JSON.parse(body)
  }

  function clientKey(request, email) {
    const forwardedFor = request.headers['x-forwarded-for']
    const ip = typeof forwardedFor === 'string' ? forwardedFor.split(',')[0].trim() : request.socket.remoteAddress
    return `${ip || 'unknown'}:${String(email || '').toLowerCase()}`
  }

  function isRateLimited(request, email) {
    const key = clientKey(request, email)
    const now = Date.now()
    const current = failedLogins.get(key)
    if (!current || current.resetAt <= now) {
      failedLogins.set(key, { count: 0, resetAt: now + 60_000 })
      return false
    }
    return current.count >= 10
  }

  function recordFailedLogin(request, email) {
    const key = clientKey(request, email)
    const current = failedLogins.get(key) || { count: 0, resetAt: Date.now() + 60_000 }
    current.count += 1
    failedLogins.set(key, current)
  }

  return async function app(request, response) {
    const requestId = request.headers['x-request-id'] || randomUUID()
    const url = new URL(request.url, 'http://localhost')

    if (request.method === 'OPTIONS') {
      response.writeHead(204, {
        ...responseHeaders(request, requestId),
        'access-control-allow-headers': 'Authorization, Content-Type, X-Request-Id',
        'access-control-allow-methods': 'GET, POST, OPTIONS',
        'access-control-max-age': '86400',
      })
      return response.end()
    }

    try {
      if (request.method === 'GET' && url.pathname === '/') {
        return send(request, response, 200, {
          service: 'ripptopia-demo-backend',
          database: false,
          startedAt,
          endpoints: ['POST /auth/login', 'POST /auth/refresh', 'GET /profile'],
        }, requestId)
      }

      if (
        request.method === 'GET' &&
        ['/health', '/health/live', '/health/ready'].includes(url.pathname)
      ) {
        return send(request, response, 200, {
          status: 'ok',
          database: 'not-used',
          timestamp: new Date().toISOString(),
        }, requestId)
      }

      if (request.method === 'POST' && url.pathname === '/auth/login') {
        const body = await readJson(request)
        const email = typeof body.email === 'string' ? body.email.trim().toLowerCase() : ''
        const password = typeof body.password === 'string' ? body.password : ''

        if (!EMAIL_PATTERN.test(email) || !password) {
          return sendError(
            request,
            response,
            400,
            'VALIDATION_ERROR',
            'Email and password are required.',
            requestId,
          )
        }

        if (isRateLimited(request, email)) {
          return sendError(
            request,
            response,
            429,
            'RATE_LIMITED',
            'Too many failed login attempts. Try again in one minute.',
            requestId,
            { 'retry-after': '60' },
          )
        }

        const account = accounts.find((candidate) => candidate.email === email)
        if (!account || !safeEqual(password, account.password)) {
          recordFailedLogin(request, email)
          return sendError(
            request,
            response,
            401,
            'INVALID_CREDENTIALS',
            'Email or password is incorrect.',
            requestId,
          )
        }

        failedLogins.delete(clientKey(request, email))
        return send(request, response, 200, {
          user: publicUser(account),
          session: issueSession(account),
        }, requestId)
      }

      if (request.method === 'POST' && url.pathname === '/auth/refresh') {
        const body = await readJson(request)
        const refreshToken = typeof body.refreshToken === 'string' ? body.refreshToken : ''
        const stored = refreshSessions.get(refreshToken)
        refreshSessions.delete(refreshToken)

        if (!stored || stored.expiresAt <= Date.now()) {
          return sendError(
            request,
            response,
            401,
            'INVALID_REFRESH_TOKEN',
            'The refresh token is invalid or expired.',
            requestId,
          )
        }

        const account = accounts.find((candidate) => candidate.id === stored.userId)
        if (!account) {
          return sendError(
            request,
            response,
            401,
            'INVALID_REFRESH_TOKEN',
            'The refresh token is invalid or expired.',
            requestId,
          )
        }

        return send(request, response, 200, { session: issueSession(account) }, requestId)
      }

      if (request.method === 'GET' && url.pathname === '/profile') {
        const auth = verifyAccessToken(bearerToken(request))
        if (auth.error) {
          const message = auth.error === 'AUTHENTICATION_REQUIRED'
            ? 'Authentication is required.'
            : auth.error === 'ACCESS_TOKEN_EXPIRED'
              ? 'The access token has expired.'
              : 'The access token is invalid.'
          return sendError(request, response, 401, auth.error, message, requestId)
        }

        return send(request, response, 200, {
          ...publicUser(auth.account),
          createdAt: startedAt,
          nextDisplayNameChangeAt: null,
        }, requestId)
      }

      return sendError(
        request,
        response,
        404,
        'ROUTE_NOT_FOUND',
        'Route not found.',
        requestId,
      )
    } catch (error) {
      if (error instanceof SyntaxError) {
        return sendError(
          request,
          response,
          400,
          'VALIDATION_ERROR',
          'Request body must be valid JSON.',
          requestId,
        )
      }
      if (error?.code === 'BODY_TOO_LARGE') {
        return sendError(
          request,
          response,
          413,
          'VALIDATION_ERROR',
          error.message,
          requestId,
        )
      }

      console.error(JSON.stringify({ level: 'error', requestId, message: error?.message }))
      return sendError(
        request,
        response,
        500,
        'INTERNAL_SERVER_ERROR',
        'Internal server error.',
        requestId,
      )
    }
  }
}
