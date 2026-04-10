# Milestones

## Milestone 0 - Product Foundation

**Goal:** Establish Strata as a product with clear identity, configuration, and
safety boundaries.

**Includes:**

- Rename Codex -> Strata
- Charter and Scope defined
- AI-optional architecture established
- `.env` configuration introduced
- Docker-based setup aligned
- Source Boundary Principle defined
- Documentation foundation created
- Config-driven system
- Installable via Docker

## Milestone 2 - Ingestion Engine

**Goal:** Strata can safely ingest and index real-world data.

**Core Capabilities:**

- Filesystem connector
- Source model (`id`, `type`, `root`, `status`)
- Ingestion pipeline
- Metadata extraction
- PostgreSQL full-text indexing
- Source boundary enforcement

**Outcome:** Strata can build a structured, queryable knowledge base from
configured sources.

## Milestone 3 - Retrieval Layer

**Goal:** Strata can retrieve relevant knowledge quickly and reliably.

**Core Capabilities:**

- Search API
- Ranking and relevance
- Snippet extraction
- Provenance tracking (`source`, `path`)
- API-first query interface

**Outcome:** Users can query Strata and get meaningful, traceable results.

## Milestone 4 - Enhancement Layer (AI Optional)

**Goal:** Improve retrieval quality using AI without making it required.

**Core Capabilities:**

- Embeddings support
- Hybrid search (FTS + vector)
- Optional summarization
- Context assembly for downstream AI systems

**Outcome:** Strata becomes AI-enhanced but remains functional without AI.

## Milestone 5 - Connectors

**Goal:** Expand beyond filesystem ingestion.

**Core Capabilities:**

- Git repository ingestion
- API-based connectors
- External system integrations

## Milestone 6 - Multi-User and Access Control

**Goal:** Enable organizational usage.

**Core Capabilities:**

- Role-based access control (RBAC)
- Multi-tenant support
- Scoped access to sources

# Roadmap

## Phase 1 - Foundation

- Product definition
- Configuration system
- Documentation

## Phase 2 - Core Functionality

- Ingestion engine
- Source modeling
- Indexing pipeline

## Phase 3 - Retrieval

- Query API
- Ranking and relevance
- Result formatting

## Phase 4 - Enhancement

- AI integration (optional)
- Hybrid search
- Context generation

## Phase 5 - Expansion

- Connectors
- Multi-user systems
- Enterprise readiness
