# Test Strategy

## Purpose

This document defines how Strata will be validated across unit, integration,
contract, and system-level checks as the product matures.

It is a validation contract for future issues, not a claim that the full test
suite already exists today.

## Current State

The current repository has baseline validation, but not broad committed
automated test coverage yet.

Implemented now:

- platform-readiness validation through `build.cmd`,
  `ops/validate-platform-readiness.ps1`, and GitHub Actions
- API readiness checks through `GET /health`
- Docker Compose configuration validation
- web build validation for `src/Codex.Web`
- manual smoke tests documented in `docs/operations.md`

Not implemented yet:

- committed unit test projects under `tests/`
- committed API contract test suites
- committed ingestion integration coverage against a disposable PostgreSQL stack
- committed performance or load validation for retrieval budgets

## Testing Goals

Strata's validation approach should protect four things first:

- source boundaries remain explicit and do not widen silently
- baseline retrieval works without optional AI services
- operator setup remains installable and verifiable from the repo
- API and web behavior remain traceable and stable enough to build on

## Test Layers

### Unit Tests

Use unit tests for deterministic, isolated logic that does not require external
infrastructure.

Target examples:

- path normalization and boundary-check helpers
- checksum and document-diff logic
- query validation and request-shape validation
- snippet extraction and ranking helper behavior
- configuration parsing and fallback logic

Expected home:

- `tests/Strata.UnitTests` or similarly named projects under `tests/`

### Integration Tests

Use integration tests when Strata behavior depends on PostgreSQL, the job
workflow, filesystem scanning, or environment-driven configuration.

Target examples:

- indexing a configured markdown source into PostgreSQL
- document upsert and deletion behavior across repeated indexing runs
- index-job claiming and lifecycle transitions
- source-boundary enforcement against configured roots
- optional-AI-off baseline behavior for the core stack

Expected home:

- `tests/Strata.IntegrationTests` with disposable database and source fixtures

### Contract Tests

Use contract tests for API and web-facing behavior that other components depend
on remaining stable.

Target examples:

- `POST /api/search` request and response shape
- document-read and index-job endpoint response contracts
- `/health` availability and readiness semantics
- web-to-API assumptions such as configured API origin and expected success or
  error states

Expected home:

- `tests/Strata.ApiContractTests`
- focused web contract or smoke coverage as the Next.js shell expands

### System and Manual Validation

Use system or manual validation for operator workflows and end-to-end checks
that span the full local deployment path.

Current baseline:

- `build.cmd`
- `build.sh`
- `ops/validate-platform-readiness.ps1`
- the readiness checklist and smoke tests in `docs/operations.md`
- GitHub Actions validation on pull requests and pushes to `main`

These checks are the current acceptance floor for repository changes until
broader automated coverage exists.

## Requirement Coverage Map

The table below maps the highest-priority requirements to existing or planned
verification.

| Requirement | Verification Type | Current Coverage | Planned Coverage Direction |
| --- | --- | --- | --- |
| Strata must be deployable as a self-hosted system | system/manual | Docker Compose startup, migrations, readiness checklist, CI validation | keep system validation in bootstrap scripts and add deployment regression checks |
| Strata must remain useful without AI services enabled | integration/system | optional AI profile is off by default; baseline stack validation and docs cover non-AI operation | add explicit integration checks that baseline retrieval passes with embedder disabled |
| AI services must enhance retrieval rather than gate core functionality | integration/contract | currently documented and configuration-driven, with optional services kept out of the default path | add fallback and hybrid retrieval tests in Milestone 4 |
| Configuration must be environment-driven and suitable for external users | integration/system | `.env.example`, Compose config validation, readiness checklist | add configuration-focused integration tests and negative-path validation |
| Ingest markdown documents from explicitly configured filesystem roots | integration/manual | current source configuration and indexing flow are documented; manual verification exists | add ingestion integration tests with fixture markdown trees |
| Store document content, title, path, checksum, and timestamps in PostgreSQL | integration | not covered by committed automated tests yet | add repository-backed ingestion assertions against disposable PostgreSQL |
| Provide full-text retrieval through an HTTP API | contract/system | manual search smoke tests and web/API readiness flow exist | add API contract and retrieval integration coverage |
| Provide document read access through an HTTP API | contract/manual | endpoint exists, but committed automated contract coverage is still missing | add document-read API contract tests |
| Provide an indexing job workflow for asynchronous ingestion | integration/manual | endpoint and worker flow exist, with manual operator checks only today | add job-lifecycle integration tests |
| Support Docker-based local and controlled-environment deployment | system/manual | Compose config validation, bootstrap validation, CI workflow | keep system validation in CI and add failure-mode checks over time |
| Target sub-second search latency for common queries | performance | no committed performance checks yet | add retrieval budget validation in Milestone 3 |
| Keep source mounts read-only | system/manual | Compose and operator verification document read-only mounts | add deployment regression checks around read-only mounting assumptions |
| Keep retrieval deterministic for equivalent inputs | unit/integration | no committed deterministic retrieval suite yet | add query and ranking regression coverage in Milestone 3 |
| Require server-side configuration for source roots | contract/integration/manual | current docs and architecture require server-side configuration; operators verify source path setup manually | add negative-path tests that reject client-controlled filesystem scope |
| Avoid hard dependencies on optional embedding infrastructure | integration/system | default stack and CI run without optional AI services | add explicit integration tests that baseline indexing and retrieval succeed without embedder services |

## Milestone Alignment

Future test work should follow the existing milestone structure instead of
introducing one-off test efforts detached from delivery goals.

- Milestone 2 should prioritize ingestion, boundary, and job-workflow coverage
- Milestone 3 should prioritize retrieval API, ranking, snippet, and
  performance coverage
- Milestone 4 should prioritize AI-optional fallback, hybrid retrieval, and
  embedder coverage
- Milestone 5 should prioritize connector contract and sync coverage
- Milestone 6 should prioritize authentication and source-scoped authorization
  coverage

## Validation Expectations For New Work

Until broader automated coverage lands, each issue should still run the
smallest relevant validation and describe it honestly in the pull request.

Use these defaults:

- docs-only changes:
  `git diff --check`
- repo or workflow changes:
  relevant bootstrap or CI-adjacent checks
- backend changes:
  `dotnet build Codex.slnx` plus focused API or indexing verification
- web changes:
  `npm install` and `npm run build` in `src/Codex.Web`
- stack or deployment changes:
  Compose config validation and the readiness validation flow

This document should be updated as committed test projects and formal coverage
maps replace today's lighter-weight validation.
