import { env } from '@/env'
import { mailer } from '@/lib/mailer'
import { RequestPasswordResetUseCase } from '@/use-cases/auth/request-password-reset'

import {
  makePasswordResetTokensRepository,
  makeUserIdentitiesRepository,
  makeUsersRepository,
} from './make-repositories'

export function makeRequestPasswordResetUseCase() {
  return new RequestPasswordResetUseCase(
    makeUsersRepository(),
    makeUserIdentitiesRepository(),
    makePasswordResetTokensRepository(),
    mailer,
    { ttlMinutes: env.PASSWORD_RESET_CODE_TTL_MINUTES, codeSecret: env.JWT_SECRET },
  )
}
