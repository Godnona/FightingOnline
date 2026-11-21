import type { PasswordHasher } from '@/lib/password-hasher'

export class FakePasswordHasher implements PasswordHasher {
  dummyVerifications = 0
  static readonly OUTDATED_PREFIX = 'outdated:'

  async hash(password: string) {
    return `hashed:${password}`
  }

  async verify(hash: string, password: string) {
    return (
      hash === `hashed:${password}` || hash === `${FakePasswordHasher.OUTDATED_PREFIX}${password}`
    )
  }

  needsRehash(hash: string) {
    return hash.startsWith(FakePasswordHasher.OUTDATED_PREFIX)
  }

  async verifyAgainstDummy(): Promise<false> {
    this.dummyVerifications++
    return false
  }
}
