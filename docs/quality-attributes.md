# Quality Attribute Profile

## Purpose

This document identifies the quality attributes that most strongly shape
Strata's architecture and roadmap.

It is intentionally concise. The goal is not to catalog every possible quality
attribute, but to make the most important tradeoffs explicit for the current
product slice and near-term roadmap.

## Context

Strata is a self-hosted retrieval product built around:

- explicit source boundaries
- PostgreSQL-backed ingestion and retrieval
- API-first access to indexed content
- optional AI enhancement rather than AI dependence

That operating model makes some attributes more important than others. In
particular, trust in source handling, predictable baseline retrieval, and
deployability matter more right now than highly optimized scale claims.

## Priority Attributes

The highest-priority quality attributes for Strata are:

- source-boundary safety
- deployability and operability
- reliability
- retrieval performance
- maintainability
- observability

## Attribute Profiles

### Source-Boundary Safety

Why it matters:

Strata's source boundary is a core trust boundary. If the system indexes
outside the configured source scope, it breaks the product's central safety
promise.

Target:

- ingestion stays anchored to explicitly configured server-side source roots
- the system does not widen filesystem access through traversal, symlink, or
  equivalent path indirection
- if boundary safety cannot be proven for a candidate path, the system should
  fail closed rather than widen access

Verification approach:

- architecture and requirements review against the source-boundary rules
- operator verification of `STRATA_SOURCES`, read-only mounts, and narrow source
  scope in `docs/operations.md`
- future ingestion and boundary test coverage in Milestone 2

Architectural impact:

- source roots are environment-driven and server-controlled
- source content is mounted read-only
- the API never accepts arbitrary filesystem paths from clients
- indexed document identity remains relative to the configured root

### Deployability And Operability

Why it matters:

Strata is a product, not an internal-only prototype. It has to be installable
and understandable in a controlled self-hosted environment.

Target:

- a new operator can configure and start the baseline stack from the repository
- the default deployment path works without optional AI services
- configuration remains environment-driven and documented

Verification approach:

- `build.cmd`, `build.sh`, and `ops/validate-platform-readiness.ps1`
- Docker Compose configuration validation
- local setup and readiness checks in `docs/operations.md`
- GitHub Actions baseline validation on pull requests and `main`

Architectural impact:

- the baseline stack is Docker Compose based
- `.env` and `.env.example` define the operator-facing configuration surface
- optional AI services are kept behind the `ai` profile rather than included in
  the default path

### Reliability

Why it matters:

Even an early retrieval product has to start cleanly, expose a usable API, and
handle its background workflow predictably enough to be trusted.

Target:

- the API reaches a ready state and exposes a health endpoint
- core retrieval and indexing job flows work consistently in local and CI
  validation
- failures are surfaced honestly rather than hidden behind optimistic docs

Verification approach:

- `GET /health` readiness checks
- platform-readiness validation and smoke tests
- future job-lifecycle and ingestion integration coverage in Milestone 2

Architectural impact:

- readiness is exposed through a dedicated health endpoint
- index work is handled through an explicit job workflow rather than hidden
  side effects
- validation is treated as part of the delivery flow, not as an optional extra

### Retrieval Performance

Why it matters:

Strata only creates value if retrieval is fast enough to feel practical for
real use.

Target:

- common full-text queries should remain sub-second for the intended early
  product scope
- baseline retrieval quality should be acceptable without optional AI services

Verification approach:

- manual retrieval smoke testing in the current repo
- future ranking, snippet, and performance-budget validation in Milestone 3

Architectural impact:

- PostgreSQL full-text search is the current retrieval engine
- the baseline path optimizes for deterministic full-text retrieval before
  hybrid or vector-enhanced behavior is added
- AI enhancement remains downstream so performance and usefulness do not depend
  on optional inference infrastructure

### Maintainability

Why it matters:

Strata's roadmap still includes major foundational work. The codebase and docs
need to stay understandable enough that new capabilities can be added without
constant rework.

Target:

- the repository structure and contribution flow stay predictable
- the documented product surface remains aligned with the implemented slice
- new work lands through narrow issue scopes rather than broad bundled changes

Verification approach:

- contribution and branch discipline in `contributing.md`
- architecture, feasibility, and operations docs kept aligned with current
  implementation
- future test projects added under the standardized `tests/` layout

Architectural impact:

- top-level layout separates product code, docs, tests, build helpers, and
  operational assets
- roadmap work is documented as roadmap work rather than implied current
  capability
- internal `Codex.*` identifiers are being managed deliberately during the
  transition to the Strata product identity

### Observability

Why it matters:

Operators need enough visibility to understand whether the stack is healthy and
whether ingestion and retrieval are behaving as expected.

Target:

- operators can tell whether the baseline stack is up and ready
- the system exposes enough logs and signals to troubleshoot setup and flow
  failures in development and controlled environments

Verification approach:

- stack health and readiness checks in `docs/operations.md`
- Docker Compose log inspection as part of readiness validation
- future structured logging and broader operational diagnostics work

Architectural impact:

- health is treated as a first-class runtime concern
- the current stack favors explicit readiness and log inspection over implied
  hidden automation
- observability is being expanded incrementally rather than overstated as a
  finished capability

## Tradeoff Summary

These quality attributes lead to a few deliberate design choices:

- Strata favors explicit source control and safety over aggressive ingestion
  flexibility
- Strata favors a usable non-AI baseline over early dependence on embeddings or
  summarization
- Strata favors a clear self-hosted operator path over infrastructure sprawl
- Strata favors honest, staged maturity over claiming enterprise-ready
  guarantees too early

## Near-Term Design Implication

Milestones 2 and 3 should continue reinforcing these attributes rather than
working around them.

That means near-term design work should prioritize:

- stronger source-boundary enforcement
- stable ingestion and job-flow behavior
- richer retrieval behavior and provenance
- clearer validation, diagnostics, and operator confidence

Later roadmap items should be evaluated by whether they preserve these quality
attributes, not just by whether they add visible features.
