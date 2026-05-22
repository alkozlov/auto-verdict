# Beads Task Workflow

## 1. Purpose

This document defines how implementation work is tracked for Auto Verdict.

Product and technical context lives in `/docs`. Beads is the operational task management system.

## 2. Task Management

Use Beads for:

- implementation tasks;
- bugs;
- infrastructure work;
- technical debt;
- documentation work;
- spikes and research;
- dependency tracking;
- implementation status.

Do not use GitHub Issues, markdown TODO files, or a fallback `.tasks/` directory for task management.

GitHub is used only for remote Git hosting and pull requests if needed.

## 3. Required Workflow

Before coding:

1. Run `bd ready`.
2. Pick the highest-priority ready task.
3. Mark the task as in progress.
4. Read the relevant documentation in `/docs`.
5. Keep the change scoped to the selected task.

During work:

1. Add Beads notes for important discoveries or blockers.
2. Add new Beads tasks for follow-up work.
3. Keep dependencies current.

Before finishing:

1. Run relevant builds, tests, and checks when possible.
2. Mark completed tasks as closed with a short completion note.
3. Run `bd sync`.
4. Commit code, documentation, and Beads updates together when they belong to the same completed task.

## 4. Labels

Use labels for area and work type.

Area labels:

- `area:frontend`
- `area:api`
- `area:processing`
- `area:ai`
- `area:payments`
- `area:storage`
- `area:messaging`
- `area:database`
- `area:devops`
- `area:docs`

Type labels:

- `type:feature`
- `type:bug`
- `type:tech-debt`
- `type:docs`
- `type:infra`
- `type:spike`

## 5. Priorities

Use Beads priorities:

- `P0`: blocks MVP foundation or end-to-end flow;
- `P1`: required for MVP;
- `P2`: important hardening or follow-up;
- `P3`: useful but not urgent;
- `P4`: optional.

## 6. Core Commands

```bash
bd ready
bd show <task-id>
bd update <task-id> --status in_progress
bd comments add <task-id> "note"
bd close <task-id> --reason "completed"
bd dep <blocker-id> --blocks <blocked-id>
bd sync
```

## 7. Human Review

The human owner should review:

- architecture changes;
- database migrations;
- payment logic;
- authentication and authorization changes;
- AI prompt changes;
- deployment changes;
- security-sensitive code.
