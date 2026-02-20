# Local Development Troubleshooting

## Compose Status and Logs

Use these first:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env ps
docker compose -f ops/docker-compose.yml --env-file ops/.env logs --tail 100
```

## Applying SQL Migrations

Run from `ops` after postgres is up:

```powershell
Get-Content -Raw migrations/001_extensions.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
Get-Content -Raw migrations/002_documents.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
Get-Content -Raw migrations/003_jobs.sql |
  docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U codex -d codex
```

Re-running the same files is safe.

## Verifying Schema Objects

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT to_regclass('public.documents');"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT to_regclass('public.index_jobs');"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT extname FROM pg_extension WHERE extname='vector';"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT to_regclass('public.ix_documents_search_vector');"
```

## Verifying Index Job Endpoints

```powershell
$create = Invoke-WebRequest -Uri http://localhost:8080/api/index-jobs `
  -Method Post -ContentType "application/json" -Body "{}"
$create.StatusCode
$job = $create.Content | ConvertFrom-Json
Invoke-WebRequest -Uri "http://localhost:8080/api/index-jobs/$($job.id)" `
  -Method Get -UseBasicParsing
```

Expected:

- POST returns `201`
- GET returns `200` for created id

## Verifying Search Endpoint

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

Expected:

- response is `200`
- results are ranked
- snippets include `<mark>` highlights

Request contract:

- `query` required, max 500 chars
- `limit` optional, default 10, bounded to 1..50
- response result fields: `id`, `path`, `title`, `snippet`, `rank`

## Verifying Document Endpoint

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

Expected:

- existing ids return `200`
- missing ids return `404`
- payload includes `id`, `path`, `title`, `content`, `updatedAt`

## Verifying Web UI

Run the web app from `src/Codex.Web`:

```powershell
Copy-Item .env.example .env.local
npm install
npm run dev
```

Expected:

- search submit calls `POST /api/search`
- clicking a result loads document details via `GET /api/documents/{id}`
- markdown content is shown as plain text in the document panel

## Verifying Indexer Claim Loop

Create one pending job and capture id:

```powershell
$jobId = docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -Atc "INSERT INTO index_jobs (status) VALUES ('pending') RETURNING id;" |
  Select-Object -First 1
$jobId
```

Wait a few seconds, then confirm claim/completion fields:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env logs --tail 80 codex-indexer
$query = "SELECT id, status, claimed_at, completed_at, worker_id, error_message " +
  "FROM index_jobs WHERE id = $jobId;"
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c $query
```

Expected:

- `status = completed`
- `claimed_at` is not null
- `completed_at` is not null
- `worker_id` is not null
- `error_message` is null

## Verifying Markdown Sync (Create/Edit/Delete)

Expected mount path:

- Atlas docs are mounted to `/atlas` in containers
- `CODEX_DOCS_ROOT_PATH` should be `/atlas`

Scanner behavior:

- Only `*.md` files are indexed (case-insensitive extension match)
- `title` uses first markdown heading, or file name when no heading exists
- Row updates occur only when file checksum changes

Create markdown file and verify row insert:

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

Edit file and verify checksum change:

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

Delete file and verify row removal:

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

## Postgres Not Healthy

Symptoms:

- `codex-postgres` stays in `starting` or `unhealthy`

Checks:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env logs postgres
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T postgres `
  psql -U codex -d codex -c "SELECT 1;"
```

Common fixes:

- Stop conflicting local postgres on port `5432`
- Reset local compose volumes when data is corrupted:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env down -v
docker compose -f ops/docker-compose.yml --env-file ops/.env up -d --build
```

## API Port Conflict (`8080`)

Symptoms:

- `codex-api` fails with bind/listen errors

Fix:

- Set `CODEX_API_PORT` in `ops/.env` to an open port, for example `18080`
- Restart compose

## Atlas Mount Path Not Found

Symptoms:

- Compose fails before start with bind-mount path errors

Fix:

- Verify `ATLAS_DOCS_HOST_PATH` in `ops/.env`
- The path is resolved relative to `ops/docker-compose.yml`
- For this repository default, use `../docs`

## Atlas Mount Must Stay Read-Only

Expected behavior:

- Writes to `/atlas` fail with `Read-only file system`

Quick check:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env exec -T codex-indexer `
  /bin/bash -ec "touch /atlas/__codex_ro_test"
```

## Service Cannot Reach Postgres

Symptoms:

- API or worker loops with postgres connection failures

Checks:

- Verify `CODEX_DB_CONNECTION_STRING` in `ops/.env`
- Confirm `Host=postgres;Port=5432`
- Review startup logs:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env logs codex-api
docker compose -f ops/docker-compose.yml --env-file ops/.env logs codex-indexer
docker compose -f ops/docker-compose.yml --env-file ops/.env logs codex-embedder
```

## Indexer Fails On Missing Docs Root

Symptoms:

- indexer logs `Configured docs root '...' does not exist.`
- jobs move to `failed` with an `error_message`

Fix:

- Verify `CODEX_DOCS_ROOT_PATH` in `ops/.env`
- Ensure the Atlas bind mount in compose points to an existing host path
- Confirm the same path exists inside container (`/atlas` by default)
