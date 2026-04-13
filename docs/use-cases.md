# Use-Case Pack

## Purpose

This document captures Strata's core user and operator workflows for the
current product slice.

It focuses on the implemented baseline and near-term platform behavior. It does
not attempt to model future connector, AI-enhanced, or multi-user scenarios as
if they already exist.

## Actors

- Operator: configures, starts, validates, and maintains a Strata deployment
- Retrieval User: searches indexed knowledge and reads returned documents
- Strata API: accepts retrieval and indexing requests
- Strata Indexer: processes indexing jobs against the configured source root

## Use Case 1: Verify Baseline Deployment Readiness

### Primary Actor

Operator

### Preconditions

- the repository is available locally
- `.env` has been created from `.env.example`
- `STRATA_SOURCES` points to an existing directory intended for ingestion
- Docker and required local tooling are available

### Main Flow

1. The operator reviews `.env` and confirms the intended source path.
2. The operator starts the baseline stack with Docker Compose.
3. The operator applies the database migrations.
4. The operator runs the documented readiness validation flow.
5. The operator verifies `/health`, search smoke tests, and web-to-API
   connectivity.

### Outcome

Strata is treated as ready for local or development use only when the readiness
checks pass and the configured source boundary is confirmed.

### Alternate Or Failure Flow

- If `STRATA_SOURCES` is too broad, missing, or points to the wrong directory,
  the operator must correct the configuration before treating the deployment as
  safe.
- If Compose validation, API readiness, or web build checks fail, the
  deployment is not considered ready and the issue must be resolved before
  milestone progress continues.

## Use Case 2: Request And Observe An Indexing Run

### Primary Actor

Operator

### Preconditions

- the baseline stack is running
- PostgreSQL migrations have been applied
- the source root is configured and mounted read-only
- the API and indexer are healthy

### Main Flow

1. The operator creates a new indexing job through `POST /api/index-jobs`.
2. The API persists the job and returns the created job payload.
3. The indexer claims the pending job.
4. The indexer scans markdown files under the configured source root.
5. The indexer upserts or deletes document records based on checksum and
   relative-path state.
6. The operator checks job status through `GET /api/index-jobs/{id}`.

### Outcome

The repository content under the configured source root is synchronized into the
database, and the job reaches a terminal status that the operator can inspect.

### Alternate Or Failure Flow

- If the source directory does not exist or is not mounted correctly, the
  indexing run cannot complete successfully and the job should surface failure
  information rather than silently succeeding.
- If path safety cannot be proven for candidate content, the intended product
  behavior is to fail closed rather than widen access beyond the configured
  boundary.
- If the indexer is unavailable, the job remains incomplete until the worker is
  restored and processing resumes.

## Use Case 3: Search Indexed Knowledge

### Primary Actor

Retrieval User

### Preconditions

- an indexing run has already populated searchable content
- the API is healthy
- the user has network access to the API or web shell

### Main Flow

1. The user submits a query through the web shell or directly to
   `POST /api/search`.
2. The API validates the request.
3. PostgreSQL executes ranked full-text retrieval.
4. The API returns matching documents with identifiers, relative paths,
   snippets, and ranking information.
5. The user reviews the returned results.

### Outcome

The user receives traceable search results without requiring optional AI
services.

### Alternate Or Failure Flow

- If the query is invalid, the API rejects the request instead of attempting an
  undefined retrieval flow.
- If indexing has not been run yet or no matching content exists, the response
  succeeds but returns no useful matches.
- If the web shell is pointed at the wrong API origin or the API is unhealthy,
  the retrieval flow fails before results can be shown.

## Use Case 4: Read A Retrieved Document

### Primary Actor

Retrieval User

### Preconditions

- the user already has a valid document identifier from search results
- the document exists in the indexed dataset
- the API is healthy

### Main Flow

1. The user selects a result from the search response.
2. The client requests `GET /api/documents/{id}`.
3. The API looks up the indexed document by identifier.
4. The API returns the document title, relative path, full content, and
   timestamp metadata.
5. The user reads the document content in the client.

### Outcome

The user can inspect the retrieved document with content and path context tied
to the indexed source material.

### Alternate Or Failure Flow

- If the document identifier no longer resolves, the API returns `404` and the
  client must show that the selected result is no longer available.
- If the API is unavailable, the document-read flow stops and the user cannot
  inspect content until service is restored.

## Use Case 5: Confirm Web-To-API Retrieval Path

### Primary Actor

Operator

### Preconditions

- the API is healthy
- the web shell dependencies are installed
- `NEXT_PUBLIC_STRATA_API_BASE_URL` is configured

### Main Flow

1. The operator starts the web shell locally.
2. The operator confirms the configured API origin matches the intended Strata
   API endpoint.
3. The operator runs a search through the web shell.
4. The operator confirms the browser can reach the API without network or CORS
   errors.
5. The operator confirms document retrieval also succeeds through the UI path.

### Outcome

The operator verifies that the web shell and API work together for the current
baseline retrieval experience.

### Alternate Or Failure Flow

- If `NEXT_PUBLIC_STRATA_API_BASE_URL` points to the wrong origin, the web
  shell cannot complete retrieval successfully.
- If browser networking or CORS fails, the UI path is not considered verified
  even when the backend is otherwise healthy.

## Notes

- These use cases describe the current baseline product slice and near-term
  platform workflows.
- They intentionally avoid modeling future multi-source, connector, AI-heavy,
  or multi-user scenarios as if they are already implemented.
- Later API, runbook, and test issues should refine these workflows rather than
  replace them with disconnected behavior descriptions.
