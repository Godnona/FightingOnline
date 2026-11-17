import { env } from '@/env'
import { logger } from '@/lib/logger'

import { InMemoryMailer } from './in-memory-mailer'
import { LogMailer } from './log-mailer'
import type { Mailer } from './mailer'
import { SmtpMailer } from './smtp-mailer'

function createMailer(): Mailer {
  switch (env.MAIL_TRANSPORT) {
    case 'memory':
      return new InMemoryMailer()
    case 'log':
      return new LogMailer(logger.child({ module: 'mailer' }), {
        from: env.MAIL_FROM,
        exposeContent: env.NODE_ENV !== 'production',
      })
    case 'smtp':
      return new SmtpMailer({
        host: env.SMTP_HOST!,
        port: env.SMTP_PORT,
        secure: env.SMTP_SECURE,
        user: env.SMTP_USER,
        password: env.SMTP_PASSWORD,
        from: env.MAIL_FROM,
      })
  }
}

export const mailer: Mailer = createMailer()

export type { MailMessage, Mailer } from './mailer'
