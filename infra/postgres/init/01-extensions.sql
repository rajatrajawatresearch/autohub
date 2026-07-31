-- Extensions AutoHub depends on. Runs once, on first cluster init.
--
-- pg_trgm    : fuzzy matching on model names ("Swift VXi" vs "SWIFT V XI") and dealer search
-- btree_gin  : lets us combine a scalar column with a JSONB GIN index in one composite index,
--              which is what makes "active listings in Pune with battery_kwh > 40" fast
-- citext     : case-insensitive email / registration-number columns without LOWER() everywhere
-- pgcrypto   : gen_random_uuid() fallback + column-level encryption helpers
-- unaccent   : Indian city/model names arrive with and without diacritics

-- btree_gist : lets an EXCLUDE constraint combine a resource id with a time range, which is how
--              double-booking is prevented for inspectors, delivery slots and rental handovers.
--              Without it that constraint cannot be created and the fallback is application-level
--              locking, which races. See docs/09-decisions-log.md D4.

CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gin;
CREATE EXTENSION IF NOT EXISTS btree_gist;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- uuid v7: time-sortable primary keys. PostgreSQL 18 ships uuidv7() natively; on 16 we implement it
-- so that index locality and key format are identical the day we upgrade and can drop this function.
CREATE OR REPLACE FUNCTION uuid_generate_v7()
RETURNS uuid
LANGUAGE plpgsql
PARALLEL SAFE
AS $$
DECLARE
    unix_ts_ms bytea;
    uuid_bytes bytea;
BEGIN
    unix_ts_ms := substring(int8send((extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3);

    -- 74 random bits after the 48-bit timestamp and the 4-bit version
    uuid_bytes := unix_ts_ms || gen_random_bytes(10);

    -- version 7
    uuid_bytes := set_byte(uuid_bytes, 6, (b'0111' || get_byte(uuid_bytes, 6)::bit(4))::bit(8)::int);
    -- RFC 4122 variant
    uuid_bytes := set_byte(uuid_bytes, 8, (b'10'   || get_byte(uuid_bytes, 8)::bit(6))::bit(8)::int);

    RETURN encode(uuid_bytes, 'hex')::uuid;
END
$$;

COMMENT ON FUNCTION uuid_generate_v7() IS
  'Time-sortable UUID v7. Replace with native uuidv7() on PostgreSQL 18+.';

-- Schemas, one per module. Isolation is enforced in code by DbContext ownership; the schemas make
-- that boundary visible in psql and make a future extract-to-service a pg_dump -n away.
CREATE SCHEMA IF NOT EXISTS platform;      -- the ten registries
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS listings;
CREATE SCHEMA IF NOT EXISTS auction;
CREATE SCHEMA IF NOT EXISTS rental;
CREATE SCHEMA IF NOT EXISTS lease;
CREATE SCHEMA IF NOT EXISTS attachment;
CREATE SCHEMA IF NOT EXISTS valuation;
CREATE SCHEMA IF NOT EXISTS inspection;
CREATE SCHEMA IF NOT EXISTS deals;
CREATE SCHEMA IF NOT EXISTS finance;
CREATE SCHEMA IF NOT EXISTS insurance;
CREATE SCHEMA IF NOT EXISTS payments;
CREATE SCHEMA IF NOT EXISTS chat;
CREATE SCHEMA IF NOT EXISTS booking;
CREATE SCHEMA IF NOT EXISTS documents;
CREATE SCHEMA IF NOT EXISTS notification;
CREATE SCHEMA IF NOT EXISTS review;
CREATE SCHEMA IF NOT EXISTS trust;
CREATE SCHEMA IF NOT EXISTS analytics;
