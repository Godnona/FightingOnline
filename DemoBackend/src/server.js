import { createServer } from 'node:http'

import { createApp } from './app.js'

const host = process.env.HOST || '0.0.0.0'
const port = Number.parseInt(process.env.PORT || '3334', 10)

if (!Number.isInteger(port) || port < 1 || port > 65_535) {
  throw new Error('PORT must be an integer between 1 and 65535')
}

if (process.env.NODE_ENV === 'production' && (!process.env.JWT_SECRET || process.env.JWT_SECRET.length < 32)) {
  throw new Error('JWT_SECRET must contain at least 32 characters in production')
}

const server = createServer(createApp())

server.listen(port, host, () => {
  console.log(JSON.stringify({
    level: 'info',
    message: 'Ripptopia demo backend is listening',
    host,
    port,
    database: false,
  }))
})

function shutdown(signal) {
  console.log(JSON.stringify({ level: 'info', message: `Received ${signal}; shutting down` }))
  server.close((error) => {
    if (error) {
      console.error(error)
      process.exitCode = 1
    }
  })
}

process.on('SIGINT', () => shutdown('SIGINT'))
process.on('SIGTERM', () => shutdown('SIGTERM'))
