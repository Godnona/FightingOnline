import { env } from '@/env'
import { passwordHasher } from '@/lib/auth-services'
import { ResetPasswordUseCase } from '@/use-cases/auth/reset-password'

import {
  makePasswordResetTokensRepository,
  makeRefreshTokensRepository,
  makeUserIdentitiesRepository,
  makeUsersRepository,
} from './make-repositories'

export function makeResetPasswordUseCase() {
  return new ResetPasswordUseCase(
    makeUsersRepository(),
    makeUserIdentitiesRepository(),
    makePasswordResetTokensRepository(),
    makeRefreshTokensRepository(),
    passwordHasher,
    { codeSecret: env.JWT_SECRET },
  )
}
