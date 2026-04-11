# Strata Playbook Gap Analysis

Audit date: 2026-04-11

Method:
- Reviewed the existing Strata docs against the senior software design playbook.
- Cross-checked those claims against the current implementation in `src/` and `ops/`.
- Validated the current build surface locally with `dotnet build Codex.slnx` and the web build before and after installing frontend dependencies.

## Executive Summary

Strata already looks like a project that has begun to think like a product instead of a prototype. The strongest areas are problem framing, scope control, the core retrieval architecture, and the decision to keep AI additive rather than foundational.

The biggest gaps are not in the core idea. They are in senior-level completeness: there is still no explicit quality-attribute profile, no delivery-process note, no use-case pack, no traceability map, and no test strategy. The current docs explain what Strata is and roughly how it works, but they do not yet provide the full decision trail or proof story the playbook expects.

The code generally supports the written product direction, and several early review gaps have already been closed since the first foundation pass. The main areas that still lag are source-boundary hardening, API contract precision, and operational depth beyond basic startup and verification.

## Strongest Areas

- `docs/charter.md` gives Strata a clear problem statement, user set, product framing, and measurable success criteria.
- `docs/scope.md` keeps v1 disciplined with explicit non-goals and a consistent retrieval-first boundary.
- `docs/requirements.md`, `docs/architecture.md`, and `docs/data-model.md` line up well around the core system: configured source root, PostgreSQL-backed indexing, search, document reads, and asynchronous index jobs.
- `docs/adr/ADR-001-project-identity.md` captures a real product decision with context and consequences instead of just renaming things.
- The implementation supports the main product thesis:
  - `src/Codex.Api` exposes the documented search, document, and index-job endpoints.
  - `src/Codex.Indexer` implements the job-claiming and markdown sync loop.
  - `ops/docker-compose.yml` enforces read-only source mounts and healthy PostgreSQL startup gating.
- The AI-optional claim is now supported in both code and deployment: search and retrieval work without embeddings, and the embedder plus Ollama are profile-gated optional services.

## Phase-by-Phase Assessment

| Area | Evidence | Status | Assessment |
|---|---|---|---|
| Problem framing | `docs/charter.md` | Strong | The product, user class, pain, and success criteria are clear enough to guide early design. |
| Feasibility | `docs/feasibility.md` | Partial | A first feasibility pass now exists, but it is still an early decision aid rather than a fully maintained planning artifact. |
| Requirements quality | `docs/requirements.md` | Partial | Requirements are clear and useful, especially around AI-optional behavior and source boundaries, but they are not prioritized, traced, or paired with validation evidence. |
| Scope discipline | `docs/scope.md` | Strong | In-scope, out-of-scope, and non-goals are clear and help prevent accidental product sprawl. |
| Quality targets | No `docs/quality-attributes.md` | Missing | Performance, reliability, observability, and maintainability are implied, but there are no explicit targets, measurements, or architecture consequences. |
| Process model | No `docs/delivery-process.md` | Missing | The docs say what to build, not how work should move or which delivery model fits the risk profile. |
| Workflows / use cases | Flows in `docs/architecture.md`, endpoint examples in `docs/api.md` | Partial | Main flows exist, but there is no use-case pack with actors, preconditions, alternate flows, or failure flows. |
| Architecture / design | `docs/architecture.md`, ADR-001 | Partial | The major containers and happy-path flows are clear, but ownership boundaries, tradeoffs, failure/retry flows, and architecture priorities are still thin. |
| Interfaces / data contracts | `docs/api.md`, `docs/data-model.md` | Partial | The public HTTP surface and database model are documented, but not as a full interface spec with ownership, validation rules, and error behavior per boundary. |
| Operations | `docs/operations.md`, `ops/docker-compose.yml`, `ops/migrations/` | Partial | Setup and smoke testing are covered, but health criteria, alerting, backups, restore, incident handling, and recovery steps are mostly absent. |
| Milestones / slices | `docs/milestones.md` | Partial | The roadmap is coherent, but milestones do not yet include acceptance criteria, explicit risk reduction, or demonstrable completion checks. |
| ADRs / traceability | ADR-001 only | Partial | One meaningful ADR exists, but there is no traceability map from goals to requirements to design to validation. |
| Verification strategy | Platform-readiness script, GitHub Actions validation workflow, no `docs/test-strategy.md` | Partial | Build and readiness validation now exist, but there is still no written test strategy and no substantive automated test suite. |

## Required Artifact Coverage

| Playbook artifact | Current repo state | Coverage |
|---|---|---|
| `charter.md` | Present as `docs/charter.md` | Present |
| `feasibility.md` | Present as `docs/feasibility.md` | Present |
| `scope.md` | Present as `docs/scope.md` | Present |
| `requirements.md` | Present as `docs/requirements.md` | Present |
| `quality-attributes.md` | Not found | Missing |
| `delivery-process.md` | Not found | Missing |
| `use-cases.md` | Not found | Missing |
| `architecture.md` | Present as `docs/architecture.md` | Present |
| `data-model.md` | Present as `docs/data-model.md` | Present |
| `interface-spec.md` | Closest substitute is `docs/api.md` | Partial |
| `operations-runbook.md` | Closest substitute is `docs/operations.md` | Partial |
| `milestones.md` | Present as `docs/milestones.md` | Present |
| `test-strategy.md` | Not found | Missing |
| `traceability.md` | Not found | Missing |

## Code-vs-Doc Findings

### Source-boundary enforcement

Status: lags documentation

What is strong:
- The runtime gets the source root from configuration, not from clients.
- `ops/docker-compose.yml` mounts the configured source path read-only.
- `src/Codex.Indexer\Indexing\MarkdownDocumentScanner.cs` stores normalized relative paths.

What is still missing:
- The scanner currently enumerates all files under the configured root and normalizes relative paths, but it does not explicitly prove symlink or reparse-point escape prevention.
- The requirements doc already hints at this by calling boundary hardening a standing product requirement, which is accurate and should remain explicit.

Assessment:
- The docs should not yet imply that source-boundary enforcement is fully hardened. The current implementation is a good foundation, not the finished safety model.

### API contract accuracy

Status: mostly aligned, one minor mismatch

What is strong:
- `POST /api/search`, `GET /api/documents/{id}`, `POST /api/index-jobs`, and `GET /api/index-jobs/{id}` all exist in code.
- Request validation is partly documented and backed by contract types such as `SearchRequest`.
- The API layer rejects empty or whitespace-only search queries.

Mismatch to fix:
- `docs/api.md` says `limit` is clamped to `1..50`. In practice, the request contract also uses `[Range(1, 50)]`, so out-of-range values are invalid requests rather than merely clamped inputs.

Assessment:
- `docs/api.md` is a useful API overview, but it is still short of a true interface specification.

### AI-optional positioning

Status: aligned more cleanly than the initial audit

What is strong:
- The charter, requirements, architecture, README, and ADR all consistently define AI as additive.
- Search and retrieval behavior live in the API, indexer, and PostgreSQL path without requiring embeddings.
- The Ollama service is correctly optional through a Compose profile.

Assessment:
- The product direction and deployment surface now tell the same AI-optional story.

### Operational readiness

Status: lags documentation expectations

What is strong:
- Configuration, migrations, startup flow, and smoke-test commands are documented.
- PostgreSQL has a Compose health check, and the API/indexer wait for database reachability before starting.

What is missing:
- The API now has a dedicated `/health` readiness path, but broader service health and recovery guidance is still thin.
- No metrics, alerts, tracing, backup, restore, or incident-response guidance exists.
- `docs/operations.md` still reads more like setup instructions plus baseline verification than a full operations runbook.

Assessment:
- This is enough for local bring-up, not yet enough for confident operations.

### Contract cleanliness

Status: improved since the initial audit

Evidence:
- The template `WeatherForecast` endpoint has been removed from the runtime surface.
- Local verification now points to `/health` instead of temporary template routes.

Assessment:
- This cleanup strengthens the public API boundary and makes the product surface more credible.

### Build and validation evidence

Status: stronger than first impression, still shallow

Validated locally:
- `dotnet build Codex.slnx` succeeds for the .NET projects.
- `npm install` and `npm run build` succeed for `src/Codex.Web`.
- A GitHub Actions workflow now automates the baseline .NET build, web build, Compose config validation, and API readiness checks.

Assessment:
- Baseline validation is materially better than it was during the first foundation review.
- The bigger concern is still the absence of automated tests and a written test strategy.

## Senior-Review Scenarios

### Can a new engineer understand the product?

Yes, mostly. The charter, README, scope, requirements, architecture docs, and feasibility memo make the product direction understandable. What is still missing is the design-completion layer around quality targets, use cases, traceability, and explicit validation strategy.

### Can an operator deploy and recover it?

An operator can likely bring the system up locally by following the setup docs. Recovery confidence is much lower because there is no real runbook for failure diagnosis, restart safety, backup, or restore.

### Can a reviewer trace intent to implementation?

Only partially. A reviewer can connect the retrieval-first story to the code, but cannot yet trace a requirement to a design decision to a validation artifact in a disciplined way.

### Are the highest-risk assumptions written down?

Some are. The AI-optional stance and source-boundary principle are explicit. The harder risks, such as full boundary hardening, operational sustainment, and proof of non-functional requirements, are still more implied than managed.

## Recommended Remediation Order

1. Add the missing design-completion artifacts with lightweight first versions:
   `quality-attributes.md`, `delivery-process.md`, `use-cases.md`, `test-strategy.md`, and `traceability.md`.
2. Split `docs/api.md` into a true interface specification or expand it so each endpoint has purpose, validation, error cases, and ownership.
3. Expand `docs/operations.md` into an actual runbook with health checks, failure scenarios, restart guidance, backup/restore, and recovery steps.
4. Tighten `docs/api.md` where the current text still overstates behaviors such as search limit handling.
5. Introduce the first automated test slice:
   one API contract test, one indexing integration test, and one documented smoke-test path that maps back to high-priority requirements.

## Bottom Line

Strata already has a credible v1 product narrative and a coherent retrieval-oriented implementation core. The next step is not a redesign. It is to finish the senior design package around that core so the project can defend its tradeoffs, prove its claims, and operate with less ambiguity.
