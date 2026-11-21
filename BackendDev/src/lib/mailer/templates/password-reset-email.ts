import type { MailMessage } from '../mailer'

export function passwordResetEmail(params: {
  to: string
  code: string
  expiresInMinutes: number
}): MailMessage {
  return {
    to: params.to,
    subject: `${params.code} is your password reset code`,
    text: [
      'We received a request to reset the password of your account.',
      '',
      `Your code: ${params.code}`,
      '',
      `Type it in the game to choose a new password. It expires in ${params.expiresInMinutes} minutes and can be used only once.`,
      '',
      'If you did not request this, you can ignore this email. Your password stays the same.',
    ].join('\n'),
  }
}
