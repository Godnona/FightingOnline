import nodemailer, { type Transporter } from 'nodemailer'

import type { MailMessage, Mailer } from './mailer'

export type SmtpMailerOptions = {
  host: string
  port: number
  secure: boolean
  user?: string
  password?: string
  from: string
}

export class SmtpMailer implements Mailer {
  private readonly transport: Transporter

  constructor(private readonly options: SmtpMailerOptions) {
    this.transport = nodemailer.createTransport({
      host: options.host,
      port: options.port,
      secure: options.secure,
      auth: options.user
        ? {
            user: options.user,
            pass: options.password,
          }
        : undefined,
    })
  }

  async send(message: MailMessage): Promise<void> {
    await this.transport.sendMail({ from: this.options.from, ...message })
  }
}
