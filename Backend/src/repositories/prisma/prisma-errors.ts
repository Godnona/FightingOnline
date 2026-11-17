import { Prisma } from '@/generated/prisma/client'
import { UniqueConstraintError } from '@/repositories/_errors/unique-constraint-error'

// with driver adapters prisma only reports the index name, so map it by hand
const uniqueIndexFields: Record<string, readonly string[]> = {
  users_display_name_normalized_key: ['displayName'],
  display_name_normalized_1: ['displayName'],
  user_identities_provider_provider_user_id_key: ['provider', 'providerUserId'],
  user_identities_user_id_provider_key: ['userId', 'provider'],
  refresh_tokens_token_hash_key: ['tokenHash'],
  password_reset_tokens_token_hash_key: ['tokenHash'],
}

function readPath(value: unknown, path: readonly string[]): unknown {
  let current = value
  for (const key of path) {
    if (typeof current !== 'object' || current === null) return undefined
    current = (current as Record<string, unknown>)[key]
  }
  return current
}

const snakeToCamel = (value: string) =>
  value.replaceAll('"', '').replace(/_([a-z])/g, (_, char: string) => char.toUpperCase())

const publicFieldName = (field: string) =>
  field === 'displayNameNormalized' ? 'displayName' : field

function uniqueViolationFields(meta: unknown): readonly string[] {
  const index = readPath(meta, ['driverAdapterError', 'cause', 'constraint', 'index'])
  if (typeof index === 'string' && uniqueIndexFields[index]) return uniqueIndexFields[index]

  const fields =
    readPath(meta, ['driverAdapterError', 'cause', 'constraint', 'fields']) ??
    readPath(meta, ['target'])
  if (Array.isArray(fields)) {
    return fields.map((field) => publicFieldName(snakeToCamel(String(field))))
  }

  if (typeof fields === 'string') {
    const known = Object.entries(uniqueIndexFields).find(([indexName]) =>
      fields.includes(indexName),
    )
    if (known) return known[1]

    return [publicFieldName(snakeToCamel(fields))]
  }

  return []
}

export function asUniqueConstraintError(error: unknown): UniqueConstraintError | undefined {
  if (error instanceof Prisma.PrismaClientKnownRequestError && error.code === 'P2002') {
    return new UniqueConstraintError(uniqueViolationFields(error.meta))
  }
  return undefined
}

export async function withUniqueConstraintTranslation<T>(operation: () => Promise<T>): Promise<T> {
  try {
    return await operation()
  } catch (error) {
    throw asUniqueConstraintError(error) ?? error
  }
}
