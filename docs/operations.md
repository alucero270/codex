# Operations

## Configuration Files

### `.env`

Use the repository-root `.env` file as the local deployment configuration
surface.

Key variables:

- `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT`
- `STRATA_API_PORT`
- `STRATA_DB_CONNECTION`
- `STRATA_SOURCES`
- `STRATA_EMBED_PROVIDER`
- `STRATA_EMBED_MODEL`
- `STRATA_LOG_LEVEL`

`STRATA_EMBED_PROVIDER` and `STRATA_EMBED_MODEL` are optional. Leave them blank
unless you plan to run the AI enhancement profile.

`.env` is for operator convenience. It is not secure secret storage and should
not be treated as a production secret-management solution.

### `.env.example`

Use `.env.example` as the shareable template for new environments.

### `src/Codex.Web/.env.example`

Use `src/Codex.Web/.env.example` as the local web-shell template when running
the frontend outside Docker.

Key variable:

- `NEXT_PUBLIC_STRATA_API_BASE_URL`

## Configuring Sources

- Set `STRATA_SOURCES` to the host path Strata is allowed to ingest
- That path is mounted read-only into the containers at the same path
- Treat it as the active source boundary for the deployment

Operational expectation:

- The directory must exist before the stack starts
- The directory should contain only content intended for ingestion
- Strata should not be granted broader filesystem access than necessary
- Operators should treat the configured path as a trust boundary, not as a
  convenience pointer to a broader parent directory

## Running With Docker Compose

From the repository root:

```powershell
Copy-Item .env.example .env
# Edit .env before first run.
docker compose -f ops/docker-compose.yml --env-file .env up -d --build
```

This default command starts the core Strata stack: PostgreSQL, API, and
indexer.

Optional AI enhancement services:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env --profile ai up -d --build
```

The `ai` profile adds the embedder worker and the local Ollama runtime. Leave
that profile disabled when you only need baseline full-text ingestion and
retrieval.

Stop the stack:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env down
```

## Applying Migrations

Run the current SQL migrations after PostgreSQL is healthy.

If you keep the sample credentials, use:

```powershell
Get-Content -Raw ops/migrations/001_extensions.sql |
  docker compose -f ops/docker-compose.yml --env-file .env exec -T postgres `
  psql -v ON_ERROR_STOP=1 -U strata -d strata
Get-Content -Raw ops/migrations/002_documents.sql |
  docker compose -f ops/docker-compose.yml --env-file .env exec -T postgres `
  psql -v ON_ERROR_STOP=1 -U strata -d strata
Get-Content -Raw ops/migrations/003_jobs.sql |
  docker compose -f ops/docker-compose.yml --env-file .env exec -T postgres `
  psql -v ON_ERROR_STOP=1 -U strata -d strata
Get-Content -Raw ops/migrations/004_job_retries.sql |
  docker compose -f ops/docker-compose.yml --env-file .env exec -T postgres `
  psql -v ON_ERROR_STOP=1 -U strata -d strata
```

If you change the PostgreSQL credentials in `.env`, update the `psql` arguments
to match.

## Verification

### Platform Readiness Checklist

Use this checklist before treating a local or development deployment as ready
for Milestone 2 ingestion work.

Everything below must pass:

- configuration sanity:
  `.env` exists at the repository root, `STRATA_DB_CONNECTION` is set, and
  `STRATA_SOURCES` points to an existing host directory intended for ingestion
- source-boundary sanity:
  the configured `STRATA_SOURCES` directory contains only intended content, and
  Compose will mount that path read-only into the API and indexer services
- backend build:
  `dotnet build Codex.slnx` succeeds without project or solution errors
- web build:
  in `src/Codex.Web`, `npm install` and `npm run build` both succeed
- Compose config:
  `docker compose -f ops/docker-compose.yml --env-file .env config` succeeds
- API readiness:
  `GET /health` returns `200 OK` after the API starts
- retrieval smoke test:
  `POST /api/search` returns a successful response with the configured API
- source path sanity:
  the running stack can start with the configured source path mounted at the
  same host path and without broader filesystem access
- web-to-API sanity:
  when the web shell is used, it can reach the configured API origin without
  browser network or CORS errors

Recommended command path:

```powershell
powershell -ExecutionPolicy Bypass -File ops/validate-platform-readiness.ps1
```

Then run the source-path and smoke-test checks in this document against your
actual `.env` values.

Milestone 1 should not be considered complete for a local or development
environment unless every checklist item above passes.

Check stack health:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env ps
docker compose -f ops/docker-compose.yml --env-file .env logs --tail 100
```

Verify API readiness:

```powershell
Invoke-WebRequest -Uri http://localhost:8080/health
```

Verify source-boundary configuration:

- confirm `STRATA_SOURCES` points to the narrowest host directory Strata should
  be allowed to ingest for this deployment
- confirm the same path is mounted into the API and indexer services as a
  read-only bind mount in `ops/docker-compose.yml`
- confirm the deployment does not rely on client requests to supply filesystem
  paths or widen ingestion scope
- confirm the configured directory does not depend on symlink or reparse-point
  traversal to reach required content; Strata now skips those entries during
  ingestion instead of following them
- confirm the running stack does not need broader host filesystem access than
  the declared source root

Verify runtime boundary enforcement:

- place or identify a symlink or reparse-point entry under `STRATA_SOURCES`
  that targets content outside the intended source root
- run an indexing job and confirm the indexer logs a warning that the entry was
  skipped instead of traversed
- confirm only markdown files that remain provably inside the configured source
  boundary are ingested

Smoke-test search:

```powershell
$body = @{
  query = "strata"
  limit = 5
} | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:8080/api/search -Method Post `
  -ContentType "application/json" -Body $body
```

Create an indexing job:

```powershell
Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs -Method Post `
  -ContentType "application/json" -Body "{}"
```

Verify index-job retry state:

- poll `GET /api/index-jobs/{id}` after creating the job
- expect `attemptCount` to increment each time the worker claims the job
- if an attempt fails and retries remain, expect the job to return to `pending`
  instead of remaining terminal immediately
- expect a terminal `failed` job to report `attemptCount == maxAttempts`
- the current retry policy is server-controlled and allows up to `3` total
  attempts per job

Platform readiness validation:

```powershell
powershell -ExecutionPolicy Bypass -File ops/validate-platform-readiness.ps1
```

This validation entry point runs:

- `dotnet build Codex.slnx`
- `npm install` and `npm run build` in `src/Codex.Web`
- `docker compose ... config` against the repository-root `.env`
- a local API boot probe against `GET /health`

GitHub Actions runs the same baseline validation on pull requests and pushes to
`main` through
`.github/workflows/validate-platform-readiness.yml`.

## Runtime Logs

Strata now emits structured JSON console logs for the API and indexer so local
and container logs can be reviewed consistently.

Current logging intent:

- retrieval logs include request context such as trace id, request path, method,
  request outcome, and safe request metadata like query length or result count
- ingestion logs include worker id, job id, configured source root, lifecycle
  events, sync counts, failure details, and boundary-skip warnings when unsafe
  filesystem entries are encountered
- document content and raw search query text should not be logged as part of
  routine retrieval diagnostics

Local verification:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env logs --tail 100 api
docker compose -f ops/docker-compose.yml --env-file .env logs --tail 100 indexer
```

What to confirm:

- API log entries are JSON-shaped and include scoped request fields for search,
  document reads, and index-job operations
- indexer log entries are JSON-shaped and include worker and job context for job
  claim, sync, completion, and failure paths
- retrieval diagnostics do not include full document content or raw query text

## Web Shell

The current web shell runs on Next.js and continues to call the Strata API
directly.

Local web workflow:

```powershell
Set-Location src/Codex.Web
Copy-Item .env.example .env.local
npm install
npm run dev
```

Production-style build verification:

```powershell
Set-Location src/Codex.Web
npm install
npm run build
```

Set `NEXT_PUBLIC_STRATA_API_BASE_URL` to the Strata API origin you want the web
shell to call. This remains an explicit API endpoint setting and does not
change Strata's source-boundary model.

Web-to-API connectivity check:

- confirm `NEXT_PUBLIC_STRATA_API_BASE_URL` points to the same API origin that
  returns `200 OK` from `/health`
- after the web shell starts, run a search and confirm the browser can reach
  `POST /api/search` on that API origin without network or CORS errors

## Operational Notes

- The current codebase still uses internal `Codex.*` runtime identifiers in some
  places; Docker and docs now present the Strata product surface
- The embedder and Ollama services are default-off optional enhancements in
  Compose
- Read-only source mounting is part of Strata's trust model
