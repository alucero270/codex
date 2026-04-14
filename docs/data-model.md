# Data Model

## Overview

Strata currently persists indexed content and indexing workflow state in
PostgreSQL.

## `sources`

Stores explicit ingestion source records for configured connectors.

### Columns

- `id`: bigint identity primary key
- `name`: server-controlled source name for operator-facing identification
- `type`: current connector type, currently `filesystem`
- `root_path`: unique configured filesystem root for the source
- `status`: lifecycle status such as `active`, `disabled`, or `error`
- `last_indexed`: timestamp reserved for source-level lifecycle tracking

### Notes

- the current product slice persists one configured filesystem source per
  deployment through the ingestion path
- source records are server-controlled and derive from deployment
  configuration, not client-supplied filesystem paths
- later Milestone 2 work will link documents and index jobs directly to source
  records and populate richer lifecycle metadata

## `documents`

Stores searchable document records.

### Columns

- `id`: bigint identity primary key
- `path`: unique text path stored relative to the configured source root
- `title`: extracted title, typically the first markdown heading
- `content`: full markdown file contents
- `checksum`: SHA-256 checksum used for change detection
- `search_vector`: generated full-text search vector
- `created_at`: creation timestamp
- `updated_at`: last update timestamp

### Notes

- `search_vector` is refreshed by trigger on insert and content/title updates
- `ix_documents_search_vector` provides GIN-backed full-text search performance
- `path` is normalized with `/` separators for cross-platform consistency

## `index_jobs`

Stores indexing work items claimed by background workers.

### Columns

- `id`: bigint identity primary key
- `status`: `pending`, `processing`, `completed`, or `failed`
- `requested_at`: job creation timestamp
- `claimed_at`: timestamp recorded when a worker claims the job
- `completed_at`: timestamp recorded on successful completion
- `attempt_count`: number of processing attempts claimed so far
- `max_attempts`: server-controlled retry ceiling for the job
- `worker_id`: worker identifier for claimed jobs
- `error_message`: truncated failure detail when processing fails

### Notes

- `ix_index_jobs_status_requested_at_id` supports pending-job polling
- Job creation is API-driven; claiming and completion are background operations
- failed attempts return to `pending` while `attempt_count < max_attempts`
- terminal `failed` state means the last allowed attempt has already been used

## Current Source Foundation

The current implementation still operates with a single configured filesystem
source root per deployment, but that source is now persisted as a first-class
record in `sources`.

Broader multi-source workflows, source-scoped job ownership, and source
management APIs remain follow-on Milestone 2 work.
