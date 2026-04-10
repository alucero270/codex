# Strata

Strata is a self-hosted knowledge ingestion and retrieval platform.

Strata is valuable without AI. AI can improve retrieval quality and downstream
workflows, but the product must remain useful when only filesystem ingestion,
metadata storage, and full-text retrieval are enabled.

## What Strata Does

- Ingests documents from explicitly configured source roots
- Indexes document content and metadata into PostgreSQL
- Serves API-first retrieval for search, document access, and indexing workflows
- Runs in Docker for local and controlled-environment deployments

## Core Principles

- AI is optional, not foundational
- Source boundaries are explicit and enforced by configuration
- Source content is mounted read-only and is never edited in place
- Retrieval must remain fast, predictable, and traceable

## Quick Start

1. Copy `.env.example` to `.env`
2. Set `STRATA_SOURCES` to a readable source directory on the host
3. Review `POSTGRES_*`, `STRATA_DB_CONNECTION`, and optional embedding settings
4. Start the stack:

```powershell
docker compose -f ops/docker-compose.yml --env-file .env up -d --build
```

5. Apply the SQL migrations in [`docs/operations.md`](docs/operations.md)

## Documentation

- [`docs/charter.md`](docs/charter.md)
- [`docs/scope.md`](docs/scope.md)
- [`docs/requirements.md`](docs/requirements.md)
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/data-model.md`](docs/data-model.md)
- [`docs/api.md`](docs/api.md)
- [`docs/operations.md`](docs/operations.md)
- [`docs/milestones.md`](docs/milestones.md)
- [`docs/adr/ADR-001-project-identity.md`](docs/adr/ADR-001-project-identity.md)

## Current Implementation Notes

- The repository still contains internal `Codex.*` project names and namespaces
  during the transition to Strata
- Product-facing docs, configuration, and deployment now use the Strata identity
- The optional embedder and Ollama profile remain enhancement paths, not
  prerequisites for search and retrieval
