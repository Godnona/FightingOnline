import { Prisma } from '@/generated/prisma/client'

function isRetryableTransactionError(error: unknown): boolean {
  if (error instanceof Prisma.PrismaClientKnownRequestError && error.code === 'P2034') return true

  const message = error instanceof Error ? error.message : String(error)
  return message.includes('WriteConflict') || message.includes('TransientTransactionError')
}

export async function withTransactionRetry<T>(operation: () => Promise<T>): Promise<T> {
  const maxAttempts = 5

  for (let attempt = 1; attempt <= maxAttempts; attempt += 1) {
    try {
      return await operation()
    } catch (error) {
      if (!isRetryableTransactionError(error) || attempt === maxAttempts) throw error
    }
  }

  throw new Error('Transaction retry loop ended unexpectedly')
}
