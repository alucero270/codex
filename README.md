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

Current note:

- `/weatherforecast` is the temporary health check endpoint.
- Add a dedicated `/health` endpoint in a follow-up API task.

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
