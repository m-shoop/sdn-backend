-- Migration: add last_activity to accounts
-- Run this against the database before deploying the corresponding application changes.
--
-- Tracks the most recent appointment-related activity for a client account.
-- Used by the GDPR cleanup service to delete accounts inactive for more than one year.
-- Activity is defined as: a new appointment booked, or an existing appointment
-- edited or cancelled.

ALTER TABLE accounts
    ADD COLUMN last_activity TIMESTAMPTZ;
