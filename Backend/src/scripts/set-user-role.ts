// usage: npm run user:set-role -- <email | display name | user id> <PLAYER | ADMIN>
import { z } from 'zod'

import { prisma } from '@/lib/prisma'
import { SetUserRoleUseCase } from '@/use-cases/admin/set-user-role'
import {
  makeUserIdentitiesRepository,
  makeUsersRepository,
} from '@/use-cases/factories/make-repositories'
import { normalizeEmail } from '@/utils/email'

const argsSchema = z.tuple([z.string().trim().min(1), z.enum(['PLAYER', 'ADMIN'])])

async function findUserId(who: string): Promise<string | null> {
  if (z.uuid().safeParse(who).success) return who

  if (who.includes('@')) {
    const identities = await makeUserIdentitiesRepository().findManyByEmail(normalizeEmail(who))
    const userIds = [...new Set(identities.map((identity) => identity.userId))]
    if (userIds.length > 1) throw new Error(`more than one account uses ${who}, pass the user id`)
    return userIds[0] ?? null
  }

  return (await makeUsersRepository().findByDisplayName(who))?.id ?? null
}

async function main() {
  const parsed = argsSchema.safeParse(process.argv.slice(2))
  if (!parsed.success) {
    throw new Error(
      'usage: npm run user:set-role -- <email | display name | user id> <PLAYER | ADMIN>',
    )
  }
  const [who, role] = parsed.data

  const userId = await findUserId(who)
  if (!userId) throw new Error(`no player found for ${who}`)

  const user = await new SetUserRoleUseCase(makeUsersRepository()).execute({
    userId,
    role,
    actorId: null,
  })
  process.stdout.write(`${user.displayName} (${user.id}) is now ${user.role}\n`)
}

try {
  await main()
} catch (error) {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`)
  process.exitCode = 1
} finally {
  await prisma.$disconnect()
}
