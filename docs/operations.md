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

## Running With Docker Compose

From the repository root:

```powershell
Copy-Item .env.example .env
# Edit .env before first run.
docker compose -f ops/docker-compose.yml --env-file .env up -d --build
```

Optional AI services:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env --profile ollama up -d
```

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
```

If you change the PostgreSQL credentials in `.env`, update the `psql` arguments
to match.

## Verification

Check stack health:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env ps
docker compose -f ops/docker-compose.yml --env-file .env logs --tail 100
```

Verify API readiness:

```powershell
Invoke-WebRequest -Uri http://localhost:8080/health
```

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

Platform readiness validation:

```powershell
powershell -ExecutionPolicy Bypass -File ops/validate-platform-readiness.ps1
```

This validation entry point runs:

- `dotnet build Codex.slnx`
- `npm install` and `npm run build` in `src/Codex.Web`
- `docker compose ... config` against the repository-root `.env`
- a local API boot probe against `GET /health`

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
- The embedder and Ollama profile are optional enhancements
- Read-only source mounting is part of Strata's trust model
