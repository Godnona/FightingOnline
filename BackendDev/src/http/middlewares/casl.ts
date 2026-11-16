import type { FastifyRequest } from 'fastify'

import { ForbiddenError } from '@/http/routes/_errors/forbidden-error'
import { type Action, defineAbilityFor, type Subject } from '@/lib/casl/define-ability'

/** goes after verifyAuthHook: [verifyAuthHook, requireCan('update', 'Role')] */
export function requireCan(action: Action, subject: Subject) {
  return async function requireCanHook(request: FastifyRequest) {
    // the role comes from the database on every request, so a promotion counts right away
    const ability = defineAbilityFor(request.getCurrentUser())
    if (ability.cannot(action, subject)) throw new ForbiddenError()
  }
}
