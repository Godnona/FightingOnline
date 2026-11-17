import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { parseEnv } from 'node:util'

import { defineConfig } from 'vitest/config'

/** Reads a single variable from .env without loading the whole file into the test process. */
function fromDotEnv(name: string): string | undefined {
  try {
    return parseEnv(readFileSync('.env', 'utf8'))[name]
  } catch {
    return undefined
  }
}

// always the test db, never DATABASE_URL from .env
const testDatabaseUrl =
  process.env.DATABASE_URL_TEST ??
  fromDotEnv('DATABASE_URL_TEST') ??
  'mongodb://127.0.0.1:27017/ripptopia_test?replicaSet=rs0&directConnection=true'

process.env.DATABASE_URL_TEST = testDatabaseUrl

export default defineConfig({
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'node',
    env: {
      NODE_ENV: 'test',
      DATABASE_URL: testDatabaseUrl,
      JWT_SECRET: 'test-only-secret-that-is-at-least-32-characters-long',
      MAIL_TRANSPORT: 'memory',
      SOCIAL_AUTH_MOCK: 'true',
      GOOGLE_CLIENT_IDS: '',
      FACEBOOK_APP_ID: '',
      FACEBOOK_APP_SECRET: '',
    },
    coverage: {
      provider: 'v8',
      include: ['src/**/*.ts'],
      exclude: ['src/generated/**', 'src/test/**', 'src/server.ts'],
    },
    projects: [
      {
        extends: true,
        test: {
          name: 'unit',
          include: ['src/test/unit/**/*.spec.ts'],
        },
      },
      {
        extends: true,
        test: {
          name: 'integration',
          include: ['src/test/integration/**/*.spec.ts'],
          globalSetup: ['src/test/setup/integration-global-setup.ts'],
          // one shared test database that gets cleared, so no parallel files
          fileParallelism: false,
        },
      },
    ],
  },
})
