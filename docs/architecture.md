# Architecture

## System Overview

Strata is a self-hosted retrieval platform built around a configured source
boundary, PostgreSQL-backed indexing, and API-first document access.

## Core Components

### Source Root

- A host directory explicitly configured through `STRATA_SOURCES`
- Mounted read-only into containers
- Treated as the only allowed ingestion boundary for the running deployment

### API Service

- Hosts HTTP endpoints for search, document reads, and index job creation
- Reads database connection and source-root configuration from environment
- Never accepts arbitrary filesystem paths from clients

### Indexer Service

- Claims pending index jobs from PostgreSQL
- Scans markdown files below the configured source root
- Computes checksums and synchronizes the `documents` table

### PostgreSQL

- Stores indexed documents and the indexing work queue
- Provides full-text search through `tsvector`, `GIN`, and ranking functions
- Supports future vector-based enhancements without requiring them in v1

### Optional Enhancement Layer

- Embedder worker and Ollama profile remain optional
- Search and document retrieval continue to function without them
- AI is a downstream enhancement layer, not a core dependency

## Request and Processing Flows

### Ingestion Flow

1. A client creates an index job through the API
2. The indexer claims the next pending job
3. The indexer scans markdown content under the configured source root
4. Documents are upserted or deleted based on relative-path and checksum state

### Retrieval Flow

1. A client submits a search query to the API
2. PostgreSQL executes ranked full-text search
3. The API returns result metadata, snippets, and document identifiers
4. A client can request full document content by identifier

## Configuration Model

- `.env` defines deployment-specific values
- Docker Compose injects those values into containers
- Runtime services consume the resulting environment variables
- Product-facing config uses `STRATA_*` names even while internal app keys still
  include temporary `Codex:*` compatibility mappings

## Source Boundary Enforcement

Strata's architecture assumes all ingestion is anchored to configured source
roots and never to user-supplied filesystem paths.

### Required Rules

- The configured root must exist before ingestion starts
- Stored document paths must remain relative to that root
- Boundary checks must prevent traversal outside declared roots
- Read-only mounts are the default deployment mode

### Current Foundation

- Compose mounts the configured source path read-only
- API and indexer receive the root path from configuration only
- Indexed document paths are normalized relative paths
