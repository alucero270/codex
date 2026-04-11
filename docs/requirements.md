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

- The configured source root is a core trust boundary for the deployment
- All ingestion must begin from a declared source root and never from a
  client-supplied filesystem path
- Candidate paths must be normalized and validated before use
- No traversal may resolve outside a declared source root
- Relative paths must remain relative to the declared source root when stored or
  returned
- Symlinks, reparse points, or equivalent filesystem indirection must not widen
  access beyond the declared source root
- If boundary safety cannot be proven for a candidate path, ingestion must fail
  closed for that path rather than widen access

### Implementation Note

The current runtime already scopes ingestion to a configured root path. Full
boundary hardening, including deeper symlink and reparse-point escape handling,
remains a standing product requirement as the ingestion engine matures. This
note does not weaken the rules above; it records that the product requirement is
already set even where deeper enforcement work is still queued.

## AI-Optional Requirement

- Full-text indexing and retrieval must work without the embedder service
- Optional embedding settings may be configured without changing baseline search
- Documentation and deployment flows must treat AI as additive capability
