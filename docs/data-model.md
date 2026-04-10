# Data Model

## Overview

Strata currently persists indexed content and indexing workflow state in
PostgreSQL.

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
- `worker_id`: worker identifier for claimed jobs
- `error_message`: truncated failure detail when processing fails

### Notes

- `ix_index_jobs_status_requested_at_id` supports pending-job polling
- Job creation is API-driven; claiming and completion are background operations

## Configured Source Model

The current implementation uses a single configured source root per deployment.
That root is not yet persisted as a first-class table.

Future milestone work may introduce explicit `sources` records, but the product
boundary already assumes every indexed path belongs to a declared source root.
