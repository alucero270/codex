# Delivery Process Note

## Purpose

This note explains how Strata work is intended to move through milestones,
issue scopes, verification, and review.

The goal is to make the project's delivery logic explicit without introducing
heavyweight ceremony.

## Chosen Delivery Model

Strata uses a milestone-guided, issue-scoped, verification-first delivery
model.

In practice, that means:

- the roadmap is organized by milestone outcomes rather than by disconnected
  feature ideas
- each change starts from exactly one narrow GitHub issue
- each issue is implemented on its own branch and reviewed through its own pull
  request
- the smallest relevant validation runs before the branch is pushed
- docs, implementation, and PR text are expected to distinguish current state
  from roadmap work

This model fits Strata's current project shape because the product is still in
foundation and early-platform stages. It needs disciplined incremental progress
more than high-throughput parallel feature delivery.

## Why This Model Fits Strata

Strata is building a retrieval product with a few sensitive constraints:

- source boundaries are a trust boundary
- the non-AI baseline must stay useful
- self-hosted deployability has to remain practical
- roadmap layers still depend on earlier design and verification work

Those constraints make broad bundled changes risky. A milestone-guided,
issue-scoped flow helps keep foundational work reviewable and keeps the repo
honest about what has actually landed.

## Working Rhythm

The intended working rhythm is:

1. Define milestone goals so each phase has a clear outcome.
2. Break those goals into narrow GitHub issues.
3. Start each issue from updated `main`.
4. Implement only that issue on a dedicated branch.
5. Run the smallest relevant local validation.
6. Open a pull request that references and closes the issue.
7. Review the change against milestone intent, verification, and scope
   discipline.
8. Merge, close the issue, and delete the completed branch.

This keeps milestone progress visible without forcing the team into long-lived
integration branches or vague batch releases.

## Verification Expectations

Verification is part of delivery, not a final cleanup step.

Current expectation:

- docs-only changes run `git diff --check`
- repo, workflow, or deployment changes run the relevant bootstrap or CI-adjacent checks
- backend changes run `dotnet build Codex.slnx` plus focused API or indexing validation
- web changes run `npm install` and `npm run build` in `src/Codex.Web`
- stack-level changes use Compose validation and the readiness validation flow

This aligns with the documented contribution flow and the current test strategy:
the repo already has a baseline proof path, even though broader automated test
coverage is still being added.

## Risks This Process Helps Manage

This delivery model is intended to reduce a few concrete risks:

- scope creep inside a single pull request
- hidden regressions caused by skipping the smallest relevant checks
- branch drift from starting work on stale `main`
- roadmap overstatement where docs or PR text imply capability that has not yet
  been implemented
- milestone confusion where foundational work and later-stage feature work are
  mixed together out of order

## Risks This Process Does Not Solve

This process improves delivery discipline, but it does not remove all project
risk.

It does not solve:

- unknown technical complexity inside future ingestion, retrieval, or access-control work
- limited engineering capacity or review bandwidth
- the absence of deeper automated coverage that still needs to be built
- the possibility that a milestone stays too broad and needs to be decomposed
  further

Process discipline helps surface those risks earlier, but it does not make them
go away on its own.

## Review Standard

Pull requests should be reviewed against four questions:

- does this change still map cleanly to exactly one issue
- does it move the current milestone forward rather than bundling unrelated
  work
- does the validation match the real risk of the change
- is the write-up honest about current state versus roadmap

If the answer to any of those questions is no, the issue likely needs to be
narrowed, split, or clarified before more work is added.
