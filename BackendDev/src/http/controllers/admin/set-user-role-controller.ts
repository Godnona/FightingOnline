import type { FastifyReply, FastifyRequest } from 'fastify'

import { presentUser } from '@/http/presenters/auth-presenters'
import type { SetUserRoleBody, SetUserRoleParams } from '@/http/routes/admin/set-user-role'
import { makeSetUserRoleUseCase } from '@/use-cases/factories/make-set-user-role-use-case'

export async function setUserRoleController(
  request: FastifyRequest<{ Params: SetUserRoleParams; Body: SetUserRoleBody }>,
  reply: FastifyReply,
) {
  const adminId = request.getCurrentUserId()
  const user = await makeSetUserRoleUseCase().execute({
    userId: request.params.userId,
    role: request.body.role,
    actorId: adminId,
  })

  request.log.info({ adminId, targetUserId: user.id, role: user.role }, 'user role changed')
  return reply.send(presentUser(user))
}
