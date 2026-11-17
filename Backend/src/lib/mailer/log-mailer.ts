import type { Logger } from 'pino'

import type { MailMessage, Mailer } from './mailer'

export class LogMailer implements Mailer {
  constructor(
    private readonly logger: Logger,
    private readonly options: { from: string; exposeContent: boolean },
  ) {}

  async send(message: MailMessage) {
    const metadata = { from: this.options.from, to: message.to, subject: message.subject }

    if (!this.options.exposeContent) {
      this.logger.warn({ mail: metadata }, 'email not sent: no email provider configured')
      return
    }

    this.logger.info({ mail: { ...metadata, text: message.text } }, 'email (log transport)')
  }
}
