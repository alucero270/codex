# Local Development Troubleshooting

## Compose Status and Logs

Use these first:

```powershell
docker compose -f ops/docker-compose.yml --env-file ops/.env ps
docker compose -f ops/docker-compose.yml --env-file ops/.env logs --tail 100
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
