# API Surface Audit

Audit date: 2026-04-11

## Summary

The current Strata runtime API surface is intentionally small. The product
contract currently consists of document retrieval, search, index-job workflow,
and a basic health endpoint. The only extra HTTP surface exposed by the API
runtime today is the development-only OpenAPI document when the app runs in the
Development environment.

This audit is a planning artifact, not an API redesign. It inventories the
current surface, identifies which endpoints belong to the intended Strata
contract, and records the remaining cleanup work to make that contract more
explicit.

## Current Endpoint Inventory

| Endpoint | Source | Current role | Contract status | Notes |
|---|---|---|---|---|
| `GET /health` | `HealthController` | basic readiness check | intended product-facing operational endpoint | Used for platform verification and local readiness checks. |
| `POST /api/search` | `SearchController` | search indexed documents | intended product-facing endpoint | Core retrieval API for the current Strata slice. |
| `GET /api/documents/{id}` | `DocumentsController` | fetch a known document | intended product-facing endpoint | Supports document viewing after search or direct lookup. |
| `POST /api/index-jobs` | `IndexJobsController` | create indexing work | intended product-facing endpoint | Current request shape is intentionally minimal for the early product slice. |
| `GET /api/index-jobs/{id}` | `IndexJobsController` | read indexing job status | intended product-facing endpoint | Supports current operational verification of indexing flow, including retry accounting. |
| `GET /openapi/v1.json` | `MapOpenApi()` in `Program.cs` | development-time API description | development-only support endpoint | Present only in Development; useful for inspection, not part of the stable product contract. |

## Non-Product Or Limited-Scope Surface

### Development-only OpenAPI document

- `GET /openapi/v1.json` exists only when the API runs in Development
- it is useful for local inspection, CI sanity checks, and future contract work
- it should not be treated as a stable product endpoint on its own

### Early-slice limitations within the current contract

- `POST /api/index-jobs` currently accepts an effectively empty request because
  source-scoped indexing has not landed yet
- `GET /health` is intentionally minimal and should remain an operational
  readiness check rather than expanding into a broad status dump
- `POST /api/search` still deserves tighter written contract language around
  validation and result semantics

## Current Public Strata API Surface

For the current Milestone 1 and early Milestone 2 boundary, the intended Strata
API contract is:

- `GET /health`
- `POST /api/search`
- `GET /api/documents/{id}`
- `POST /api/index-jobs`
- `GET /api/index-jobs/{id}`

Everything else should be treated as development support behavior or future
work until explicitly documented otherwise.

## Cleanup Plan

1. Keep `docs/api.md` aligned to only the intended product-facing endpoints and
   operationally relevant behavior.
2. Continue treating `GET /openapi/v1.json` as development support behavior,
   not as a public product promise.
3. Tighten the written contract for `POST /api/search`, especially around input
   validation and the current limit-handling behavior.
4. Revisit the index-job endpoints once source-aware ingestion lands so the
   request and status model match the future source-scoped workflow.
5. Add richer API contract documentation later if the repo introduces new
   public endpoints or external integrator expectations.
