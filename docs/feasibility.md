# Feasibility Memo

## Purpose

Assess whether the current Strata roadmap is feasible given the product's
implemented slice, operating model, and stated constraints.

This memo evaluates feasibility for the near-term roadmap, especially
Milestones 2 and 3. It does not assume that later roadmap items are already
implemented.

## Current Baseline

Strata already has a credible early product slice:

- self-hosted deployment through Docker Compose
- filesystem-based ingestion from an explicitly configured source root
- PostgreSQL-backed indexing and full-text retrieval
- HTTP APIs for search, document reads, and indexing jobs
- a minimal web UI for search and document viewing

That baseline matters because Strata is intended to be a product, not an
internal-only tool or a roadmap artifact. The system already demonstrates value
without requiring AI, and its source-boundary model is central to user trust.

## Technical Feasibility

Technical feasibility is moderate to high for the current roadmap.

The core architecture is coherent: a configured source boundary feeds an
indexer, PostgreSQL provides durable storage and full-text search, and the API
exposes retrieval without requiring any AI-dependent path. That gives Strata a
sound foundation for Milestone 2 and Milestone 3 work.

The main technical constraint is that the source boundary is both a product
feature and a trust boundary. Future work has to preserve that boundary while
adding source modeling, stronger path validation, provenance, and richer
retrieval behavior.

Concrete risks and failure modes:

- A symlink escape, path traversal gap, or weak path-normalization rule could
  allow indexing outside the configured source root and break Strata's core
  trust model.
- If optional embedding or hybrid retrieval paths become required for acceptable
  baseline results, the implementation would drift away from the AI-optional
  product rule.
- Expanding to multi-source or connector-based ingestion before the single-root
  boundary is fully hardened could multiply complexity faster than the current
  model can safely absorb.

## Operational Feasibility

Operational feasibility is moderate for controlled environments.

The current Docker-based setup, environment-driven configuration, and
PostgreSQL-backed services are realistic for self-hosted deployments. Operators
can understand the main components and bring the system up with conventional
infrastructure.

Operational maturity is still early, though. The repository documents
configuration and smoke tests, but broader operator runbooks, health coverage,
and failure-handling guidance are still maturing.

Concrete risks and failure modes:

- An operator could configure `STRATA_SOURCES` too broadly and unintentionally
  ingest sensitive or irrelevant content because the source boundary is
  deployment-controlled.
- Incomplete runbooks or migration guidance could lead to partial startup,
  stuck indexing jobs, or slow recovery during incidents.
- If deployment guidance starts implying enterprise-grade isolation before
  health checks, observability, and access controls are ready, operators may
  trust the system beyond its current operational maturity.

## Economic and Value Feasibility

Economic and value feasibility is moderate to high if Strata stays focused on
its current product promise.

The repo already supports a useful baseline proposition: ingest controlled
filesystem content, index it predictably, and retrieve it without requiring AI
infrastructure. That is a credible value story for teams that want traceable
retrieval before paying the cost of more advanced augmentation layers.

The economic case weakens if Strata tries to compete on speculative future
capabilities rather than the value of safe, explainable retrieval today.

Concrete risks and failure modes:

- If search quality, provenance, or document access remain too limited, users
  may not perceive enough value from the non-AI baseline to justify adoption.
- If optional AI services become necessary to make the product feel useful,
  deployment cost and complexity could rise before the product's core value is
  proven.
- If the roadmap expands faster than the implemented slice, documentation and PR
  messaging could overstate readiness and create expectation debt.

## Schedule Feasibility

Schedule feasibility is moderate, with strong dependence on scope discipline.

The roadmap is feasible if the next phases continue building from the existing
slice: harden ingestion boundaries, improve source modeling, and make retrieval
more complete before expanding into broader connectors or AI-heavy workflows.

The schedule becomes less feasible when roadmap layers are pursued out of order.
Strata still needs boundary hardening, broader verification, and clearer API and
operational maturity before later milestones can land safely.

Concrete risks and failure modes:

- Starting connector work or AI-heavy features before Milestones 2 and 3 are
  solid could create rework in storage models, APIs, and trust guarantees.
- A broad milestone definition could hide several dependent subprojects and make
  delivery appear on track while critical prerequisites remain incomplete.
- If documentation treats aspirational capabilities as near-term commitments,
  the team may inherit schedule pressure that the current implementation cannot
  support.

## Legal and Compliance Feasibility

Legal and compliance feasibility is limited but workable for the current
product posture.

Strata's self-hosted model, explicit source boundaries, and read-only source
mounting are helpful design choices because they reduce accidental data movement
and keep deployment control with the operator. Those choices align well with a
product that needs to respect source ownership and traceability.

That said, the current repository does not yet demonstrate the full set of
controls needed for stronger compliance claims. It should be described as a
product foundation for controlled environments, not as a turnkey compliance
solution.

Concrete risks and failure modes:

- If a source root includes regulated, confidential, or out-of-scope material,
  Strata may ingest it without the policy checks or approval workflows some
  organizations require.
- Without mature access control, audit, and retention features, deployments may
  fail internal compliance review for broader organizational use.
- If future docs imply that AI or connectors can access content across source
  boundaries without equivalent controls, Strata could undermine its own trust
  position.

## Resource Feasibility

Resource feasibility is moderate for a small team if the work remains narrow.

The current product slice uses familiar components and avoids overcommitting to
specialized infrastructure. That keeps the immediate roadmap relatively
resource-efficient compared with systems that require vector infrastructure,
complex orchestration, or multi-tenant administration from day one.

The resource picture changes quickly if Strata tries to pursue boundary
hardening, retrieval quality, connectors, AI enhancement, UI maturity, and
enterprise controls at the same time.

Concrete risks and failure modes:

- A small team could become a bottleneck if the same contributors are
  responsible for ingestion safety, API design, operations, and product-facing
  UX simultaneously.
- Security and boundary-hardening work may be delayed if it competes directly
  with visible roadmap items that feel more feature-rich but are less
  foundational.
- If roadmap scope expands without matching engineering and review capacity,
  the likely outcome is partial implementations instead of trustworthy product
  milestones.

## Recommendation

Proceed with the current roadmap, but keep the near-term focus on Milestones 2
and 3: source-aware ingestion, boundary hardening, provenance, and stable
retrieval behavior.

Do not treat AI enhancement, broader connectors, or organizational access
control as near-term proof points for feasibility. Strata is feasible because it
already has a product-shaped, AI-optional foundation. The most credible path is
to deepen that foundation, preserve source boundaries as a trust boundary, and
describe later milestones as roadmap work rather than current capability.
