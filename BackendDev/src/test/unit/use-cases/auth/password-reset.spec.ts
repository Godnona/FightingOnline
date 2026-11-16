import { beforeEach, describe, expect, it } from 'vitest'

import { InvalidPasswordResetCodeError } from '@/use-cases/_errors/auth-errors'
import { MAX_RESET_CODES_PER_HOUR } from '@/use-cases/auth/request-password-reset'
import { MAX_RESET_CODE_ATTEMPTS } from '@/use-cases/auth/reset-password'
import {
  type AuthTestContext,
  makeAuthTestContext,
  resetCodeFromMailer,
} from '@/test/helpers/make-auth-test-context'

const EMAIL = 'player@example.com'

describe('password reset use cases', () => {
  let ctx: AuthTestContext

  beforeEach(async () => {
    ctx = makeAuthTestContext()
    await ctx.register.execute({ email: EMAIL, password: 'old-password' })
  })

  const requestCode = async () => {
    await ctx.requestPasswordReset.execute({ email: EMAIL })
    return resetCodeFromMailer(ctx.mailer, EMAIL)
  }

  const wrongCode = (code: string) => (code === '000000' ? '111111' : '000000')

  describe('RequestPasswordResetUseCase', () => {
    it('emails a 6 digit code and stores only its hash', async () => {
      await ctx.requestPasswordReset.execute({ email: 'Player@Example.com' })

      const message = ctx.mailer.lastMessageTo(EMAIL)
      const code = resetCodeFromMailer(ctx.mailer, EMAIL)
      expect(code).toMatch(/^\d{6}$/)
      expect(message?.subject).toBe(`${code} is your password reset code`)
      expect(message?.text).toContain('30 minutes')
      expect(ctx.db.passwordResetTokens).toHaveLength(1)
      expect(ctx.db.passwordResetTokens[0]?.tokenHash).toMatch(/^[0-9a-f]{64}$/)
      expect(ctx.db.passwordResetTokens[0]?.tokenHash).not.toContain(code)
    })

    it('does nothing for unknown emails and banned accounts', async () => {
      await ctx.requestPasswordReset.execute({ email: 'ghost@example.com' })
      ctx.db.users[0]!.status = 'BANNED'
      await ctx.requestPasswordReset.execute({ email: EMAIL })

      expect(ctx.mailer.outbox).toHaveLength(0)
      expect(ctx.db.passwordResetTokens).toHaveLength(0)
    })

    it('kills the previous code when a new one is requested', async () => {
      const first = await requestCode()
      const second = await requestCode()

      if (first !== second) {
        await expect(
          ctx.resetPassword.execute({ email: EMAIL, code: first, newPassword: 'new-password' }),
        ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
      }
      await expect(
        ctx.resetPassword.execute({ email: EMAIL, code: second, newPassword: 'new-password' }),
      ).resolves.toBeUndefined()
    })

    it(`sends at most ${MAX_RESET_CODES_PER_HOUR} codes per hour`, async () => {
      for (let i = 0; i < MAX_RESET_CODES_PER_HOUR + 2; i++) {
        await ctx.requestPasswordReset.execute({ email: EMAIL })
      }

      expect(ctx.mailer.outbox).toHaveLength(MAX_RESET_CODES_PER_HOUR)
    })
  })

  describe('ResetPasswordUseCase', () => {
    it('updates the password and revokes all sessions', async () => {
      const code = await requestCode()

      await ctx.resetPassword.execute({
        email: 'PLAYER@example.com',
        code,
        newPassword: 'new-password',
      })

      const identity = ctx.db.identities[0]
      expect(identity?.passwordHash).toBe('hashed:new-password')
      expect(identity?.emailVerifiedAt).toBeInstanceOf(Date)
      expect(ctx.db.refreshTokens.every((t) => t.revokedReason === 'PASSWORD_CHANGED')).toBe(true)
      await expect(
        ctx.login.execute({ email: EMAIL, password: 'new-password' }),
      ).resolves.toBeTruthy()
    })

    it('accepts each code only once', async () => {
      const code = await requestCode()
      await ctx.resetPassword.execute({ email: EMAIL, code, newPassword: 'new-password' })

      await expect(
        ctx.resetPassword.execute({ email: EMAIL, code, newPassword: 'another-password' }),
      ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
    })

    it(`kills the code after ${MAX_RESET_CODE_ATTEMPTS} wrong attempts`, async () => {
      const code = await requestCode()

      for (let i = 0; i < MAX_RESET_CODE_ATTEMPTS; i++) {
        await expect(
          ctx.resetPassword.execute({
            email: EMAIL,
            code: wrongCode(code),
            newPassword: 'x-password',
          }),
        ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
      }

      await expect(
        ctx.resetPassword.execute({ email: EMAIL, code, newPassword: 'new-password' }),
      ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
      expect(ctx.db.passwordResetTokens[0]?.usedAt).toBeInstanceOf(Date)
    })

    it('still accepts the right code after a few wrong ones', async () => {
      const code = await requestCode()
      await expect(
        ctx.resetPassword.execute({
          email: EMAIL,
          code: wrongCode(code),
          newPassword: 'x-password',
        }),
      ).rejects.toThrow()

      await expect(
        ctx.resetPassword.execute({ email: EMAIL, code, newPassword: 'new-password' }),
      ).resolves.toBeUndefined()
    })

    it('rejects expired codes and codes of another account', async () => {
      const code = await requestCode()
      await ctx.register.execute({ email: 'other@example.com', password: 'other-password' })

      await expect(
        ctx.resetPassword.execute({
          email: 'other@example.com',
          code,
          newPassword: 'new-password',
        }),
      ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)

      ctx.db.passwordResetTokens[0]!.expiresAt = new Date(Date.now() - 1000)
      await expect(
        ctx.resetPassword.execute({ email: EMAIL, code, newPassword: 'new-password' }),
      ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
      await expect(
        ctx.resetPassword.execute({
          email: 'ghost@example.com',
          code,
          newPassword: 'new-password',
        }),
      ).rejects.toBeInstanceOf(InvalidPasswordResetCodeError)
    })
  })
})
