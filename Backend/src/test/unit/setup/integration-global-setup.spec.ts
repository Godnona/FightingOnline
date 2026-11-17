import { afterEach, describe, expect, it } from 'vitest'

import setup from '@/test/setup/integration-global-setup'

describe('integration global setup', () => {
  const original = process.env.DATABASE_URL_TEST

  afterEach(() => {
    if (original === undefined) delete process.env.DATABASE_URL_TEST
    else process.env.DATABASE_URL_TEST = original
  })

  it('refuses to migrate a database whose name does not end with _test', () => {
    process.env.DATABASE_URL_TEST = 'mongodb://localhost:27017/ripptopia'

    expect(() => setup()).toThrow(/must end with "_test"/)
  })

  it('fails when DATABASE_URL_TEST is missing', () => {
    delete process.env.DATABASE_URL_TEST

    expect(() => setup()).toThrow(/DATABASE_URL_TEST is not set/)
  })
})
