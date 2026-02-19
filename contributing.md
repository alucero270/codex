# Contributing to Codex

Codex follows strict commit conventions to ensure a clean, readable, and maintainable
project history. All commits must follow the format and rules below.

---

# Git Commit Guidelines

We have precise rules for how commit messages are formatted. This makes the project
history easier to understand and navigate across GitHub and other Git tools.

Each commit message consists of:

- Header (required)
- Body (optional but strongly encouraged)
- Footer (optional)

No line may exceed 100 characters.

---

## Commit Message Format

<type>(<scope>): <subject>

<BLANK LINE>
<body>

<BLANK LINE>
<footer>

The header is mandatory.
The scope is optional but strongly recommended.

---

## Revert

If a commit reverts a previous commit, use the following format:

revert: <original header>

In the body include:

This reverts commit <hash>.

Where <hash> is the SHA of the reverted commit.

---

## Type

Must be one of the following:

feat:     A new feature  
fix:      A bug fix  
refactor: A code change that neither fixes a bug nor adds a feature  
perf:     A code change that improves performance  
test:     Adding or updating tests  
docs:     Documentation-only changes  
style:    Formatting, whitespace, or CSS-only changes (no logic changes)  
chore:    Build process or tooling changes  
content:  A change in user-visible content  
ci:       CI/CD workflow changes  

---

## Scope

Scope should describe the part of the system affected.

Approved scopes for Codex:

api  
indexer  
embedder  
db  
web  
ops  
docs  
ci  
repo  

Use lowercase.

Examples:

feat(api): add full-text search endpoint  
chore(ops): add docker compose file  
feat(indexer): implement job claim loop  

---

## Subject

The subject must:

- Use imperative present tense ("add", not "added" or "adds")
- Not capitalize the first letter
- Not end with a period
- Be concise and descriptive

Examples:

✔ add full-text search endpoint  
✔ implement chunk checksum comparison  
✘ Added search endpoint.  

---

## Body

Use imperative present tense.

Explain:

- What changed
- Why it changed
- How it differs from previous behavior

Keep lines under 100 characters.

Example:

Implement ranked full-text search using ts_rank and ts_headline.

Previously search returned unordered results without highlighting.
This change improves relevance and UX while maintaining sub-second response time.

---

## Footer

Used for:

- Referencing GitHub issues
- Declaring breaking changes

Issue reference examples:

Closes #12  
Refs #14  

Breaking changes must begin with:

BREAKING CHANGE:

Example:

BREAKING CHANGE: rename documents.path to documents.file_path

---

# Branching Strategy

- One branch per issue
- Branch name format:

issue/<number>-short-description

Example:

issue/12-fts-search  
issue/18-docker-compose  

- Merge via Pull Request
- Squash only if commits are noisy
- Do not push directly to main

---

# Commit Examples

feat(db): add initial documents schema

Create documents table with checksum and FTS support.
Add trigger to auto-update search_vector column.

Closes #4


feat(indexer): claim index jobs using skip locked

Implement job claiming via FOR UPDATE SKIP LOCKED.
Prevent multiple workers from processing same job.

Closes #5


chore(ops): add docker compose for local development

Add postgres, api, indexer, and embedder services.
Configure healthchecks and environment variables.

Closes #3
