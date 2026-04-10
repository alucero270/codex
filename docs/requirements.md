# Requirements

## Product Requirements

- Strata must be deployable as a self-hosted system
- Strata must remain useful without AI services enabled
- AI services must enhance retrieval rather than gate core functionality
- Configuration must be environment-driven and suitable for external users

## Functional Requirements

- Ingest markdown documents from explicitly configured filesystem roots
- Store document content, title, path, checksum, and timestamps in PostgreSQL
- Provide full-text retrieval through an HTTP API
- Provide document read access through an HTTP API
- Provide an indexing job workflow for asynchronous ingestion
- Support Docker-based local and controlled-environment deployment

## Non-Functional Requirements

- Target sub-second search latency for common queries
- Keep source mounts read-only
- Keep retrieval deterministic for equivalent inputs
- Require server-side configuration for source roots
- Avoid hard dependencies on optional embedding infrastructure

## Configuration Requirements

- `.env` is the local operator-facing configuration surface
- `.env.example` must document the required variables without embedding secrets
- Docker Compose must consume `POSTGRES_*` and `STRATA_*` variables
- Runtime services may internally map Strata config values to existing app keys
  during the Codex-to-Strata transition

## Source Boundary Principle

Strata must only ingest and operate on explicitly configured sources.

### Rules

- No traversal outside configured root paths
- Normalize and validate all paths
- Do not follow symlinks outside allowed boundaries
- All ingestion must originate from declared sources
- Source roots must be configured server-side, never supplied by clients

### Implementation Note

The current runtime already scopes ingestion to a configured root path. Full
boundary hardening, including symlink escape handling, remains a standing
product requirement as the ingestion engine matures.

## AI-Optional Requirement

- Full-text indexing and retrieval must work without the embedder service
- Optional embedding settings may be configured without changing baseline search
- Documentation and deployment flows must treat AI as additive capability
