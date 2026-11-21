-- Invariants Prisma cannot express in the schema, enforced by the database itself.

-- Only e-mail/password identities may store a password hash
ALTER TABLE "user_identities"
  ADD CONSTRAINT "user_identities_password_only_for_email"
  CHECK ("provider" = 'EMAIL' OR "password_hash" IS NULL);

-- E-mail/password identities always have an e-mail address
ALTER TABLE "user_identities"
  ADD CONSTRAINT "user_identities_email_required_for_email"
  CHECK ("provider" <> 'EMAIL' OR "email" IS NOT NULL);
