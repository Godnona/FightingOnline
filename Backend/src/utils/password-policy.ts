export const PASSWORD_MIN_LENGTH = 8
export const PASSWORD_MAX_LENGTH = 128

const COMMON_PASSWORDS = new Set([
  '12345678',
  '123456789',
  '1234567890',
  '87654321',
  '11111111',
  '00000000',
  'password',
  'password1',
  'password123',
  'passw0rd',
  'qwertyui',
  'qwerty123',
  'qwertyuiop',
  'asdfghjk',
  'iloveyou',
  'sunshine',
  'princess',
  'football',
  'baseball',
  'welcome1',
  'abc12345',
  'abcd1234',
  'letmein1',
  'trustno1',
  'senha123',
  'ripptopia',
])

export function isCommonPassword(password: string): boolean {
  return COMMON_PASSWORDS.has(password.toLowerCase())
}
