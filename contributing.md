# Contributing to Strata

Strata follows strict commit conventions to keep history readable, reviewable,
and issue-scoped.

---

# Git Commit Guidelines

Each commit message consists of:

- Header (required)
- Body (optional but strongly encouraged)
- Footer (optional)

No line may exceed 100 characters.

## Commit Message Format

| type(scope): subject |
| ---- |
| Body |
| Footer |

The header is mandatory. The scope is optional but strongly recommended.

## Revert

If a commit reverts a previous commit, use the following format:

revert: <original header>

In the body include:

This reverts commit <hash>.

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

## Scope

Scope should describe the part of the system affected.

Approved scopes for Strata:

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

## Subject

The subject must:

- Use imperative present tense ("add", not "added" or "adds")
- Not capitalize the first letter
- Not end with a period
- Be concise and descriptive

## Body

Use imperative present tense.

Explain:

- What changed
- Why it changed
- How it differs from previous behavior

Keep lines under 100 characters.

## Footer

Used for:

- Referencing GitHub issues
- Declaring breaking changes

Issue reference examples:

Closes #12
Refs #14

Breaking changes must begin with:

BREAKING CHANGE:

# Branching Strategy

- One branch per issue
- Branch name format: `issue/<number>-short-description`
- Merge via Pull Request
- Squash only if commits are noisy
- Do not push directly to `main`

# Issue Workflow

Every change should map to a single GitHub issue with:

- Summary
- Scope
- Acceptance Criteria
- Notes (optional)

Keep pull requests small, reviewable, and aligned to one issue at a time.
