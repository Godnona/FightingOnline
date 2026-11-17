import { buildApp } from '@/app'
import { env } from '@/env'

const app = buildApp()

async function shutdown(signal: NodeJS.Signals) {
  app.log.info({ signal }, 'shutting down')
  try {
    await app.close()
    process.exit(0)
  } catch (error) {
    app.log.error({ err: error }, 'error during shutdown')
    process.exit(1)
  }
}

for (const signal of ['SIGINT', 'SIGTERM'] as const) {
  process.once(signal, () => void shutdown(signal))
}

try {
  await app.listen({ host: env.HOST, port: env.PORT })
} catch (error) {
  app.log.fatal({ err: error }, 'failed to start server')
  process.exit(1)
}
