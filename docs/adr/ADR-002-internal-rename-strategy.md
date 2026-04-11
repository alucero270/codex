# ADR-002: Internal Rename Strategy

## Status

Accepted

## Context

The product-facing identity has already shifted from Codex to Strata. That
change is visible in the README, operator docs, Docker surface, environment
contract, and GitHub repository identity.

The codebase, solution, and several runtime identifiers still use `Codex.*`
names. Those identifiers are currently functional and are already referenced by
project files, build commands, paths, and compatibility mappings.

An immediate full internal rename would add churn across the solution,
repository history, CI workflows, docs, and local development habits at the
same time that Milestone 1 is focused on platform readiness. That work would
increase review surface and regression risk without directly improving the
current Strata product slice.

## Decision

- Keep the Strata identity on product-facing surfaces now
- Keep existing `Codex.*` internal identifiers temporarily where they do not
  block product-facing clarity or platform readiness
- Treat deeper internal rename work as a future, explicit follow-up rather than
  an implicit requirement of every cleanup issue
- Prefer incremental rename work only when a change is already needed for
  correctness, safety, developer usability, or product-facing confusion

## Triggers For A Deeper Internal Rename

A broader internal rename becomes justified when one or more of the following
conditions are true:

- internal `Codex.*` names create real operator or contributor confusion that
  cannot be addressed cleanly through documentation
- build, CI, packaging, or deployment workflows are made harder to maintain by
  mixed Codex/Strata naming
- public artifacts such as package names, binaries, container images, or API
  metadata need stable Strata-aligned identifiers end to end
- the project is already undergoing a coordinated refactor where rename churn
  can be absorbed without multiplying review risk
- roadmap work requires clearer module ownership or naming consistency across
  API, indexer, embedder, and web components

## Consequences

- The current repo can keep moving on readiness and ingestion work without
  forcing a large rename-first pause
- Product-facing docs remain honest that the internal code still uses
  transitional `Codex.*` identifiers
- Future rename work should be planned as a dedicated slice with explicit scope,
  validation, and migration notes rather than as incidental cleanup
- Contributors should not treat mixed naming as a signal to rename files or
  projects opportunistically during unrelated issue work
