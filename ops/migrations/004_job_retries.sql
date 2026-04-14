-- Persist retry accounting so ingestion jobs can retry predictably.
ALTER TABLE index_jobs
    ADD COLUMN IF NOT EXISTS attempt_count INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS max_attempts INTEGER NOT NULL DEFAULT 3;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_index_jobs_attempt_count_non_negative'
    ) THEN
        ALTER TABLE index_jobs
            ADD CONSTRAINT ck_index_jobs_attempt_count_non_negative
                CHECK (attempt_count >= 0);
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_index_jobs_max_attempts_positive'
    ) THEN
        ALTER TABLE index_jobs
            ADD CONSTRAINT ck_index_jobs_max_attempts_positive
                CHECK (max_attempts >= 1);
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_index_jobs_attempt_count_within_limit'
    ) THEN
        ALTER TABLE index_jobs
            ADD CONSTRAINT ck_index_jobs_attempt_count_within_limit
                CHECK (attempt_count <= max_attempts);
    END IF;
END
$$;
