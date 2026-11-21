import type { MailMessage, Mailer } from './mailer'

export class InMemoryMailer implements Mailer {
  readonly outbox: MailMessage[] = []

  async send(message: MailMessage) {
    this.outbox.push(message)
  }

  lastMessageTo(to: string): MailMessage | undefined {
    return this.outbox.findLast((message) => message.to === to)
  }

  clear() {
    this.outbox.length = 0
  }
}
