-- Add unique constraint on (email, role) to prevent duplicate accounts
ALTER TABLE core.accounts
    ADD CONSTRAINT accounts_email_role_unique UNIQUE (email, role);
