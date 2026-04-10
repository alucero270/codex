# ADR-001: Project Identity

## Status

Accepted

## Context

The repository originated as an internal prototype called Codex. The system is
now being positioned as a product-grade platform intended for external,
self-hosted deployments.

The product needs a clearer identity, a stable configuration surface, and a
definition that does not depend on AI services to justify its value.

## Decision

- The product name is **Strata**
- Strata is defined as a self-hosted knowledge ingestion and retrieval platform
- Strata is useful without AI; AI is an optional enhancement layer
- Product-facing documentation, environment variables, and Docker deployment
  should use the Strata identity
- Internal code identifiers may temporarily remain `Codex.*` while the product
  surface transitions safely

## Consequences

- Documentation and operator workflows become clearer for external users
- Configuration becomes product-facing even before a full internal rename
- Future work can rename internal projects incrementally without blocking the
  product foundation
- Retrieval-first architecture remains valid regardless of AI availability
