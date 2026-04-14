# API

## Overview

Strata currently exposes a small HTTP API for indexing and retrieval.

## `GET /health`

Return a minimal readiness response for API boot verification.

### Response

```json
{
  "status": "ok"
}
```

## `POST /api/search`

Search indexed documents using PostgreSQL full-text search.

### Request

```json
{
  "query": "incident runbook",
  "limit": 10
}
```

### Rules

- `query` is required and limited to 500 characters
- `limit` defaults to `10` and is clamped to `1..50`

### Response

```json
{
  "query": "incident runbook",
  "limit": 10,
  "results": [
    {
      "id": 12,
      "path": "runbooks/incident.md",
      "title": "Incident Runbook",
      "snippet": "Use <mark>incident</mark> procedure ...",
      "rank": 0.72
    }
  ]
}
```

## `GET /api/documents/{id}`

Return the full indexed document for a known identifier.

### Response

```json
{
  "id": 12,
  "path": "runbooks/incident.md",
  "title": "Incident Runbook",
  "content": "# Incident Runbook\n...",
  "updatedAt": "2026-04-10T08:30:00Z"
}
```

### Status Codes

- `200` for an existing document
- `404` when the document id is not found

## `POST /api/index-jobs`

Create a new indexing job.

### Request

```json
{}
```

### Response

Returns `201 Created` with the created job payload.

### Behavior

- retry policy is server-controlled for the current product slice
- each job currently allows up to `3` total processing attempts
- jobs return retry accounting metadata so operators can tell whether a
  processing failure was the first attempt or a later retry

## `GET /api/index-jobs/{id}`

Fetch the current status of an indexing job.

### Response Shape

```json
{
  "id": 5,
  "status": "completed",
  "requestedAt": "2026-04-10T08:20:00Z",
  "claimedAt": "2026-04-10T08:20:02Z",
  "completedAt": "2026-04-10T08:20:05Z",
  "attemptCount": 1,
  "maxAttempts": 3,
  "workerId": "host:1234:abcd",
  "errorMessage": null,
  "stats": null
}
```

### Retry Semantics

- `attemptCount` increments each time a worker claims the job for processing
- a failed attempt returns the job to `pending` while `attemptCount` is still
  below `maxAttempts`
- a job becomes terminally `failed` only when the final allowed attempt fails

## Notes

- Use `/health` for basic readiness checks and product verification
- All API behavior remains valid without optional AI services enabled
