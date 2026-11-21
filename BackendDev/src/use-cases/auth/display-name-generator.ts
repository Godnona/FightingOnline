import type { UsersRepository } from '@/repositories/users-repository'
import { generateDisplayNameCandidate } from '@/utils/display-name'

export class DisplayNameGenerator {
  constructor(
    private readonly users: UsersRepository,
    private readonly candidate: () => string = generateDisplayNameCandidate,
    private readonly maxAttempts = 10,
  ) {}

  async generate(): Promise<string> {
    for (let attempt = 0; attempt < this.maxAttempts; attempt++) {
      const name = this.candidate()
      if (!(await this.users.findByDisplayName(name))) return name
    }
    throw new Error(`could not find a free display name after ${this.maxAttempts} attempts`)
  }
}
