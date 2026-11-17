import argon2 from 'argon2'

export interface PasswordHasher {
  hash(password: string): Promise<string>
  verify(hash: string, password: string): Promise<boolean>
  needsRehash(hash: string): boolean
  verifyAgainstDummy(password: string): Promise<false>
}

const ARGON2_OPTIONS = {
  type: argon2.argon2id,
  memoryCost: 19_456,
  timeCost: 2,
  parallelism: 1,
} as const

export class Argon2PasswordHasher implements PasswordHasher {
  private dummyHash?: Promise<string>

  hash(password: string) {
    return argon2.hash(password, ARGON2_OPTIONS)
  }

  async verify(hash: string, password: string) {
    try {
      return await argon2.verify(hash, password)
    } catch {
      return false
    }
  }

  needsRehash(hash: string) {
    return argon2.needsRehash(hash, ARGON2_OPTIONS)
  }

  async verifyAgainstDummy(password: string): Promise<false> {
    this.dummyHash ??= this.hash('dummy-password-for-timing-protection')
    await this.verify(await this.dummyHash, password)
    return false
  }
}
