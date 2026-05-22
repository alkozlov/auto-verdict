# AI Agent Workflow with GitHub Issues

## 1. Purpose

This document defines how AI coding assistants should plan, pick up, execute, and report development work for this project.

Markdown documents remain the source of product and architectural context. GitHub Issues are the source of truth for executable work.

The goal is to avoid unstructured markdown task lists and give both the human developer and AI agents a shared operational workflow.

## 2. Source of Truth

### Product and Technical Context

Stored in `/docs`:

```txt
01_PRD.md
02_FUNCTIONAL_REQUIREMENTS.md
03_NON_FUNCTIONAL_REQUIREMENTS.md
04_ARCHITECTURE.md
05_AI_PIPELINE.md
06_DATA_MODEL.md
07_REPOSITORY_AND_DEPLOYMENT.md
08_BACKLOG.md
09_AI_AGENT_WORKFLOW.md
```

### Tasks and Implementation State

Stored in GitHub Issues.

GitHub Issues are used for:

- feature tasks;
- bugs;
- infrastructure work;
- technical debt;
- documentation work;
- spikes/research tasks;
- implementation state.

Markdown backlog files are not the operational task tracker.

## 3. Required GitHub Labels

Create the following labels.

### Type Labels

```txt
type:feature
type:bug
type:tech-debt
type:docs
type:infra
type:spike
```

### Area Labels

```txt
area:frontend
area:api
area:processing
area:ai
area:payments
area:storage
area:messaging
area:database
area:devops
area:docs
```

### Status Labels

```txt
status:ready
status:blocked
status:in-progress
status:needs-review
```

### Priority Labels

```txt
priority:p0
priority:p1
priority:p2
priority:p3
```

## 4. Milestones

Suggested milestones:

```txt
MVP-0: Project Skeleton
MVP-1: Auth and Users
MVP-2: Car Check Creation
MVP-3: Messaging and Processing
MVP-4: AI Report
MVP-5: Payments
MVP-6: Deployment Hardening
```

## 5. Issue Template

Each implementation issue should include:

```md
## Goal

What should be implemented?

## Context

Relevant docs, requirements, and architectural constraints.

## Scope

What is included?

## Out of Scope

What must not be done in this task?

## Acceptance Criteria

- [ ] Criterion 1
- [ ] Criterion 2
- [ ] Criterion 3

## Implementation Notes

Optional technical notes.

## Testing Notes

How should this be tested?
```

## 6. AI Agent Rules

AI agents must follow these rules:

1. Read the linked issue first.
2. Read relevant docs in `/docs`.
3. Do not invent requirements that contradict documentation.
4. Keep changes scoped to the issue.
5. Create or use a branch named after the issue.
6. Run relevant tests/builds before reporting completion when possible.
7. Update the issue or PR with what changed and how it was verified.
8. Do not silently change architecture decisions.
9. If a task is blocked, comment on the issue instead of guessing.
10. Do not use markdown files as a hidden task list.

## 7. Recommended Agent Commands

List ready tasks:

```bash
gh issue list --state open --label status:ready
```

View an issue:

```bash
gh issue view 12
```

Mark issue as in progress:

```bash
gh issue edit 12 --remove-label status:ready --add-label status:in-progress
```

Create a branch:

```bash
git checkout -b feature/12-google-auth
```

Create a pull request:

```bash
gh pr create --fill --base main
```

Link issue in commit or PR:

```txt
Closes #12
```

## 8. Branch Naming

Use:

```txt
feature/<issue-number>-short-name
fix/<issue-number>-short-name
infra/<issue-number>-short-name
docs/<issue-number>-short-name
```

Examples:

```txt
feature/12-google-auth
infra/8-docker-compose-skeleton
fix/24-stripe-webhook-idempotency
```

## 9. Pull Request Template

```md
## Summary

What changed?

## Linked Issue

Closes #...

## Testing

- [ ] Build passed
- [ ] Tests passed
- [ ] Manual verification completed

## Notes

Any migration, config, or follow-up notes.
```

## 10. Recommended First Issues

Create GitHub labels, milestones, and initial GitHub Issues from `08_BACKLOG.md` before implementation starts.

Start with:

1. Create monorepo structure.
2. Add base documentation.
3. Add Docker Compose skeleton.
4. Create .NET solution.
5. Configure PostgreSQL and EF Core.
6. Configure NATS JetStream.
7. Configure SeaweedFS.
8. Create Next.js app.
9. Add `AGENTS.md`.

## 11. Suggested AGENTS.md

Place this file at the repository root.

```md
# AGENTS.md

## Project

Auto Verdict is a SaaS product for AI-assisted preliminary analysis of used car listings.

## Primary Rules

- Use GitHub Issues as the source of truth for implementation work.
- Read `/docs` before making architectural or product decisions.
- Keep changes scoped to the assigned issue.
- Do not introduce new infrastructure without an issue and documentation update.
- Backend request validation must use FluentValidation only.
- API must not perform long-running AI analysis.
- ProcessingService owns asynchronous analysis work.
- NATS JetStream is the message bus.
- SeaweedFS is the object storage backend.
- Claude is the first AI provider, behind an abstraction.

## Before Coding

1. Read the issue.
2. Read relevant docs.
3. Identify affected service(s).
4. Check existing patterns.

## Before Finishing

1. Run relevant build/tests when possible.
2. Update docs if behavior or architecture changed.
3. Open or update PR.
4. Link PR to issue.
```

## 12. Human Review

The human owner should review:

- architecture changes;
- database migrations;
- payment logic;
- authentication and authorization changes;
- AI prompt changes;
- deployment changes;
- security-sensitive code.

## 13. Task Size Guidance

Prefer small issues that can be completed in one focused session.

Good issue size:

- one endpoint;
- one table/migration;
- one service adapter;
- one frontend page;
- one integration slice.

Avoid issues like:

```txt
Implement all payments
Build full backend
Create whole frontend
```

Break them into smaller issues.
