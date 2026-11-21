import type { FastifyInstance } from 'fastify'
import type { ZodTypeProvider } from 'fastify-type-provider-zod'
import { z } from 'zod'

import { setUserRoleController } from '@/http/controllers/admin/set-user-role-controller'
import { verifyAuthHook } from '@/http/middlewares/auth'
import { requireCan } from '@/http/middlewares/casl'
import { userResponseSchema } from '@/http/schemas/auth-schemas'

export const setUserRoleParamsSchema = z.object({ userId: z.uuid() })
export const setUserRoleBodySchema = z.object({ role: z.enum(['PLAYER', 'ADMIN']) })

export type SetUserRoleParams = z.infer<typeof setUserRoleParamsSchema>
export type SetUserRoleBody = z.infer<typeof setUserRoleBodySchema>

export async function setUserRole(app: FastifyInstance) {
  app.withTypeProvider<ZodTypeProvider>().patch(
    '/users/:userId/role',
    {
      preHandler: [verifyAuthHook, requireCan('update', 'Role')],
      schema: {
        tags: ['admin'],
        summary: 'Make a player an admin or back to a player (admins only)',
        description: 'An admin cannot change their own role.',
        security: [{ bearerAuth: [] }],
        params: setUserRoleParamsSchema,
        body: setUserRoleBodySchema,
        response: { 200: userResponseSchema },
      },
    },
    setUserRoleController,
  )
}
