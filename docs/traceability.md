# Traceability Map

## Purpose

This document links Strata's product goals to concrete requirements, design
elements, and validation paths.

It is meant to stay lightweight and review-friendly. It is not a compliance
matrix or a replacement for the milestone issue flow.

## Goal References

Use these goal identifiers in the traceability table below.

| Goal ID | Goal |
| --- | --- |
| G1 | Provide fast, unified retrieval over controlled knowledge sources |
| G2 | Preserve source boundaries as a trust boundary |
| G3 | Stay useful without requiring AI services |
| G4 | Remain installable and operable in self-hosted environments |
| G5 | Keep the product roadmap honest and incrementally verifiable |

These goals come from the project charter, scope, requirements, and current
product framing.

## Traceability Table

| Req ID | Requirement | Goal Link | Design Elements | Validation Path | Current Status |
| --- | --- | --- | --- | --- | --- |
| R1 | Strata must be deployable as a self-hosted system | G4 | `README.md`, `docs/operations.md`, `ops/docker-compose.yml`, `build.cmd`, `build.sh` | platform-readiness validation, Compose config validation, local setup/runbook checks | Implemented baseline; operational depth still maturing |
| R2 | Strata must remain useful without AI services enabled | G1, G3 | `README.md`, `docs/architecture.md`, `docs/quality-attributes.md`, `ops/docker-compose.yml` | baseline stack validation without `ai` profile, future integration coverage for non-AI retrieval | Implemented baseline; richer proof planned |
| R3 | AI services must enhance retrieval rather than gate core functionality | G3, G5 | `README.md`, `docs/architecture.md`, `docs/feasibility.md`, `docs/quality-attributes.md` | config and deployment review today, future fallback and hybrid tests in Milestone 4 | Defined and partially enforced by deployment shape |
| R4 | Configuration must be environment-driven and suitable for external users | G4, G5 | `.env.example`, `docs/operations.md`, `docs/architecture.md` | Compose config validation, operator setup checks, future negative-path config tests | Implemented baseline |
| R5 | Ingest markdown documents from explicitly configured filesystem roots | G1, G2 | `docs/requirements.md`, `docs/architecture.md`, `src/Codex.Indexer`, `src/Codex.Api` | manual ingestion checks today, future ingestion integration tests in Milestone 2 | Implemented baseline; broader verification planned |
| R6 | Store document content, title, path, checksum, and timestamps in PostgreSQL | G1 | `docs/data-model.md`, `docs/architecture.md`, `src/Codex.Indexer`, `src/Codex.Api` | .NET build today, future repository-backed ingestion assertions | Implemented baseline; automated proof still planned |
| R7 | Provide full-text retrieval through an HTTP API | G1 | `docs/api.md`, `docs/architecture.md`, `src/Codex.Api`, `src/Codex.Web` | `POST /api/search` smoke tests, web-to-API checks, future contract and retrieval tests | Implemented baseline |
| R8 | Provide document read access through an HTTP API | G1 | `docs/api.md`, `docs/architecture.md`, `src/Codex.Api`, `src/Codex.Web` | manual endpoint verification today, future API contract tests | Implemented baseline; automated proof still planned |
| R9 | Provide an indexing job workflow for asynchronous ingestion | G1, G4 | `docs/api.md`, `docs/architecture.md`, `src/Codex.Api`, `src/Codex.Indexer` | manual operator verification today, future job-lifecycle integration tests | Implemented baseline; broader reliability coverage planned |
| R10 | Support Docker-based local and controlled-environment deployment | G4 | `ops/docker-compose.yml`, `docs/operations.md`, `build.cmd`, `build.sh`, `.github/workflows/validate-platform-readiness.yml` | bootstrap validation, CI workflow, Compose config checks | Implemented baseline |
| R11 | Target sub-second search latency for common queries | G1 | `docs/charter.md`, `docs/requirements.md`, `docs/quality-attributes.md` | manual smoke checks today, future retrieval performance budget validation in Milestone 3 | Planned verification |
| R12 | Keep source mounts read-only | G2, G4 | `ops/docker-compose.yml`, `docs/operations.md`, `docs/architecture.md` | operator verification and Compose review today, future deployment regression checks | Implemented baseline |
| R13 | Keep retrieval deterministic for equivalent inputs | G1, G5 | `docs/requirements.md`, `docs/test-strategy.md`, future retrieval design issues | future unit and integration regression coverage in Milestone 3 | Planned |
| R14 | Require server-side configuration for source roots | G2, G4 | `docs/requirements.md`, `docs/architecture.md`, `docs/operations.md`, `src/Codex.Api` | operator verification today, future negative-path tests rejecting client-controlled filesystem scope | Implemented baseline; stronger automated proof planned |
| R15 | Avoid hard dependencies on optional embedding infrastructure | G3, G4 | `README.md`, `docs/architecture.md`, `docs/quality-attributes.md`, `ops/docker-compose.yml` | baseline validation without optional AI services, future integration coverage | Implemented baseline |
| R16 | Keep source-boundary rules explicit and fail closed when safety cannot be proven | G2, G5 | `docs/requirements.md`, `docs/architecture.md`, `docs/operations.md` | architecture and operator review today, future boundary enforcement tests in Milestone 2 | Defined now; full enforcement still maturing |

## Reading The Status Column

Status values are intentionally simple:

- `Implemented baseline`: the repo already demonstrates the requirement in a
  usable early form
- `Defined and partially enforced`: the requirement clearly shapes the current
  design, but broader verification or enforcement work is still ahead
- `Planned` or `Planned verification`: the requirement is accepted, but the
  stronger proof path is scheduled for later milestone work

The status column is meant to keep design-package review honest about what is
already present versus what is still roadmap work.

## Review Notes

Use this map when reviewing:

- whether goals are still connected to concrete requirements
- whether major requirements have a corresponding design home in the repo
- whether each important requirement has a visible proof path
- whether milestone work is closing traceability gaps or only adding features

If this table becomes hard to maintain, it should be simplified rather than
expanded into a heavyweight governance artifact.
