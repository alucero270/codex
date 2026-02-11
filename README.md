# Codex — Internal Knowledge & Search Platform

Codex is Pantheon’s internal knowledge system. It indexes authoritative documentation from Atlas (read-only) and provides fast, ranked search, document viewing, and usage analytics.

Codex is designed as **infrastructure**, not a wiki clone.

---

## Purpose

- Centralize operational knowledge (architecture, ADRs, procedures, runbooks)
- Eliminate tribal knowledge and slow documentation discovery
- Serve as the documentation backbone for all Pantheon systems

---

## Architecture Overview

- **Runtime:** Prometheus (compute)
- **Source of Truth:** Atlas (read-only document mounts)
- **Database:** PostgreSQL
- **Indexing:** File-based ingestion with checksum change detection
- **Search:** PostgreSQL full-text search (semantic search optional later)

Codex does **not** modify source documents.

---

## Core Features

- Markdown document ingestion
- Folder-based taxonomy (`architecture/`, `procedures/`, etc.)
- Ranked full-text search with snippets
- Document viewer
- Manual and scheduled re-indexing
- Usage analytics (searches, views)

---

## Non-Goals

- Editing documents
- Acting as authoritative storage
- AI-first design

---

## Deployment

Codex runs as containerized services on Prometheus and mounts Atlas content read-only.

```bash
docker compose up -d
