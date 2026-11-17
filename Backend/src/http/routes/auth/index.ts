import type { FastifyInstance } from 'fastify'

import { forgotPassword } from './forgot-password'
import { linkIdentity } from './link-identity'
import { listIdentities } from './list-identities'
import { login } from './login'
import { logout } from './logout'
import { logoutAll } from './logout-all'
import { refresh } from './refresh'
import { register } from './register'
import { resetPassword } from './reset-password'
import { socialLogin } from './social-login'
import { unlinkIdentity } from './unlink-identity'

export async function authRoutes(app: FastifyInstance) {
  await app.register(register)
  await app.register(login)
  await app.register(socialLogin)
  await app.register(refresh)
  await app.register(logout)
  await app.register(logoutAll)
  await app.register(forgotPassword)
  await app.register(resetPassword)
  await app.register(listIdentities)
  await app.register(linkIdentity)
  await app.register(unlinkIdentity)
}
