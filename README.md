# Codex - Internal Knowledge and Search Platform

Codex is Pantheon's internal knowledge system and a hybrid retrieval platform
supporting CAG and optional RAG workflows.

Codex indexes authoritative Atlas documentation (read-only) and provides ranked
search, document viewing, and usage analytics.

Codex is infrastructure, not a wiki clone.

---

## Purpose

- Centralize operational knowledge (architecture, ADRs, procedures, runbooks)
- Eliminate tribal knowledge and slow documentation discovery
- Serve as the documentation backbone for Pantheon systems

---

## Architecture Overview

- Runtime: Prometheus (compute)
- Source of Truth: Atlas (read-only document mounts)
- Database: PostgreSQL (pgvector-ready)
- Indexing: File-based ingestion with checksum change detection
- Search: PostgreSQL full-text search (semantic search optional later)

Codex does not modify source documents.

---

## Core Features

- Markdown document ingestion
- Folder-based taxonomy (`architecture/`, `procedures/`, etc.)
- Ranked full-text search with snippets
- Document viewer
- Manual and scheduled re-indexing
- Usage analytics (searches and views)

---

## Non-Goals

- Editing documents
- Acting as authoritative storage
- AI-first design

---

## Deployment

Codex runs as containerized services on Prometheus and mounts Atlas content as
read-only.

---

## Local Development (Docker Compose)

### Prerequisites

- Docker Desktop or Docker Engine with Compose v2
- Ports `5432` and `8080` available

### Start

From the repository root:

```powershell
Copy-Item ops/.env.example ops/.env
docker compose -f ops/docker-compose.yml --env-file ops/.env up -d --build
```

Or from the `ops` directory:

```powershell
cd ops
Copy-Item .env.example .env
docker compose up -d --build
```

### Apply Phase 1 Migrations

Run from the `ops` directory:

```powershell
Get-Content -Raw migrations/001_extensions.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
Get-Content -Raw migrations/002_documents.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
Get-Content -Raw migrations/003_jobs.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
```

These migrations are idempotent and safe to re-run.

### Verify

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env ps
Invoke-WebRequest http://localhost:8080/weatherforecast -UseBasicParsing
docker compose -f ops/docker-compose.yml --env-file ops/.env logs --tail 50
```

Verify schema objects:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT to_regclass('public.documents');"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT to_regclass('public.index_jobs');"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT extname FROM pg_extension WHERE extname='vector';"
```

Verify index job API endpoints:

```powershell
$create = Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs `
  -Method Post -ContentType "application/json" -Body "{}"
$create.StatusCode
$job = $create.Content | ConvertFrom-Json
Invoke-WebRequest -Uri "http://localhost:8080/api/index-jobs/$($job.id)" `
  -Method Get -UseBasicParsing
```

Expected results:

- `codex-postgres` is `healthy`
- `codex-api`, `codex-indexer`, and `codex-embedder` are `Up`
- `/weatherforecast` returns HTTP 200
- `POST /api/index-jobs` returns `201 Created`
- `GET /api/index-jobs/{id}` returns `200 OK` for existing ids

Verify search API:

```powershell
$body = @{
  query = "index jobs postgres"
  limit = 10
} | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:8080/api/search -Method Post `
  -ContentType "application/json" -Body $body
```

```bash
curl -sS -X POST "http://localhost:8080/api/search" \
  -H "Content-Type: application/json" \
  -d '{"query":"index jobs postgres","limit":10}'
```

Expected search response:

- Results are ordered by relevance rank descending, then path ascending
- `snippet` includes highlighted `<mark>...</mark>` matches

Request contract:

- `query` required, max 500 chars
- `limit` optional, default 10, bounded to 1..50
- Response fields per result: `id`, `path`, `title`, `snippet`, `rank`

Verify document API:

```powershell
Invoke-RestMethod -Uri http://localhost:8080/api/documents/1 -Method Get
try {
  Invoke-WebRequest -Uri http://localhost:8080/api/documents/999999 -Method Get -UseBasicParsing
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

```bash
curl -sS "http://localhost:8080/api/documents/1"
curl -i "http://localhost:8080/api/documents/999999"
```

Expected document response:

- `200` for existing ids, `404` for missing ids
- Includes `id`, `path`, `title`, `content`, and `updatedAt`

Current note:

- `/weatherforecast` is the temporary health check endpoint.
- Add a dedicated `/health` endpoint in a follow-up API task.

### Verify Indexer Claim Loop (Issue #9)

Create a pending job and capture the new id:

```powershell
$jobId = docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -Atc "INSERT INTO index_jobs (status) VALUES ('pending') RETURNING id;"
$jobId
```

Confirm the worker claimed and completed it:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env logs --tail 80 codex-indexer
$query = "SELECT id, status, claimed_at, completed_at, worker_id, error_message " +
  "FROM index_jobs WHERE id = $jobId;"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c $query
```

Expected:

- `status = completed`
- `claimed_at` and `completed_at` are populated
- `worker_id` is populated
- `error_message` is `NULL`

### Verify Markdown Sync (Create/Edit/Delete)

Atlas docs are expected at `/atlas` inside containers (`CODEX_DOCS_ROOT_PATH=/atlas`).
The `documents.path` value is stored relative to that root using `/` separators.

Scanner behavior:

- Only `*.md` files are indexed (case-insensitive extension match)
- `title` uses first markdown heading, or file name when no heading exists
- Row updates occur only when file checksum changes

Create a markdown file and trigger indexing:

```powershell
$testFile = "docs/__sync-test.md"
Set-Content -Path $testFile -Value "# Sync Test`nVersion 1"
Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs -Method Post `
  -ContentType "application/json" -Body "{}" | Out-Null
Start-Sleep -Seconds 3
$query = "SELECT path, title, checksum FROM documents " +
  "WHERE path='__sync-test.md';"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c $query
```

Edit the same file and trigger indexing again:

```powershell
Set-Content -Path $testFile -Value "# Sync Test`nVersion 2"
Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs -Method Post `
  -ContentType "application/json" -Body "{}" | Out-Null
Start-Sleep -Seconds 3
$query = "SELECT path, checksum, updated_at FROM documents " +
  "WHERE path='__sync-test.md';"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c $query
```

Delete the file and trigger indexing one more time:

```powershell
Remove-Item -Path $testFile
Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs -Method Post `
  -ContentType "application/json" -Body "{}" | Out-Null
Start-Sleep -Seconds 3
$query = "SELECT COUNT(*) AS remaining FROM documents " +
  "WHERE path='__sync-test.md';"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c $query
```

Expected:

- Create inserts one row for `__sync-test.md`
- Edit changes the stored checksum
- Delete removes the row (`remaining = 0`)

## Web UI (Codex.Web)

Run the React + TypeScript UI locally:

```powershell
cd src/Codex.Web
npm install
```

Set API base URL (defaults to `http://localhost:8080` if omitted):

```powershell
Copy-Item .env.example .env.local
# Optional: edit .env.local if your API is on a different host/port.
```

Start the dev server:

```powershell
npm run dev
```

The UI provides:

- Search input and submit (`POST /api/search`)
- Ranked results list with loading/error/empty states
- Click result to load document view (`GET /api/documents/{id}`)
- Markdown displayed as plain text for MVP (no markdown renderer dependency)

Quick API smoke checks before opening the UI:

```powershell
Invoke-RestMethod -Uri http://localhost:8080/api/search -Method Post `
  -ContentType "application/json" -Body '{"query":"local development","limit":5}'
Invoke-RestMethod -Uri http://localhost:8080/api/documents/1 -Method Get
```

### Optional Ollama Profile

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env --profile ollama up -d
```

### Stop

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env down
Remove-Item ops/.env
```

---

## Troubleshooting

See `docs/local-dev.md` for common local setup and compose troubleshooting.
