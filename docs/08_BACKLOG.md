# MVP Backlog

This backlog is optimized for AI-assisted development with Claude Code, Codex, or similar coding agents.

Executable tasks should be created as GitHub Issues. This document is a planning seed, not the operational task source of truth.

## Epic 1: Repository Setup

### TASK-001: Create monorepo structure

Create the repository structure described in `07_REPOSITORY_AND_DEPLOYMENT.md`.

### TASK-002: Add base documentation

Add all markdown docs into `/docs`.

### TASK-003: Add `.gitignore` and `.editorconfig`

Add sensible defaults for .NET, Node.js, Next.js, Docker, and local environment files.

### TASK-004: Create Docker Compose skeleton

Add services:

- frontend;
- api;
- processing-service;
- postgres;
- nats;
- seaweedfs.

### TASK-005: Add `.env.example`

Document required environment variables.

## Epic 2: Backend Foundation

### TASK-010: Create .NET solution

Create:

- AutoVerdict.Api;
- AutoVerdict.ProcessingService;
- AutoVerdict.Contracts;
- AutoVerdict.Domain;
- AutoVerdict.Application;
- AutoVerdict.Infrastructure.

### TASK-011: Configure PostgreSQL and EF Core

Add database context, migrations, and basic connection configuration.

### TASK-012: Add health endpoints

Add health endpoints for API and ProcessingService.

### TASK-013: Add structured logging

Configure structured logging with correlation IDs where practical.

### TASK-014: Add FluentValidation setup

Configure FluentValidation for API request validation.

### TASK-015: Add backend-owned auth foundation

Prepare backend session/JWT configuration and authorization primitives for Google OAuth.

## Epic 3: Authentication and Users

### TASK-020: Implement Google authentication

Add backend-owned Google OAuth sign-in, user profile creation/update, and backend-issued auth cookie or JWT.

### TASK-021: Store external auth account

Persist OAuth provider identity.

### TASK-022: Add authenticated user endpoint

Return current user profile and credit balance.

### TASK-023: Grant initial free credits

On first registration, grant configurable initial free credits.

## Epic 4: Credits

### TASK-030: Implement credit balance model

Create `user_credits` and `credit_ledger`.

### TASK-031: Implement credit consumption

Consume one credit when a check is submitted.

Credit consumption must be transactional with check creation, input persistence, uploaded file metadata persistence, and outbox message insertion.

### TASK-032: Prevent negative balance

Ensure credit consumption is transactional and safe.

### TASK-033: Implement technical refund operation

Allow automatic refunds for clearly classified technical failures and manual protected refunds for internal/admin use.

## Epic 5: Storage

### TASK-040: Configure SeaweedFS in Docker Compose

Add SeaweedFS services and S3-compatible endpoint.

### TASK-041: Implement object storage abstraction

Create `IObjectStorage` interface.

### TASK-042: Implement SeaweedFS/S3 storage adapter

Use generic S3-compatible operations.

### TASK-043: Store uploaded file metadata

Create `uploaded_files` table and persistence logic.

### TASK-044: Validate uploads

Validate file size, content type, and file count.

Uploads are API-mediated for MVP. Presigned direct browser uploads are out of scope.

## Epic 6: Car Checks

### TASK-050: Implement create check endpoint

Accept listing data, optional vehicle metadata, and screenshots.

### TASK-051: Validate create check request

Use FluentValidation only.

### TASK-052: Persist check input

Store car check data and normalized input.

### TASK-053: Implement check list endpoint

List current user's previous checks.

### TASK-054: Implement check details endpoint

Return check status and report if available.

## Epic 7: Messaging and Outbox

### TASK-060: Configure NATS JetStream

Create stream and durable consumer configuration.

### TASK-061: Define message contracts

Create `StartCarCheckAnalysis` contract.

### TASK-062: Implement transactional outbox

Write outbox message in the same transaction as check creation.

### TASK-063: Implement outbox publisher

Publish pending outbox messages to NATS JetStream.

### TASK-064: Implement inbox/idempotency records

Prevent duplicate processing of the same message.

## Epic 8: ProcessingService

### TASK-070: Create NATS consumer

Consume car check analysis messages from durable consumer.

### TASK-071: Load check input and files

Load data from PostgreSQL and SeaweedFS.

### TASK-072: Mark processing status

Update check status to `Processing` and later to `Completed` or `Failed`.

### TASK-073: Implement retry/failure handling

Retry temporary failures and store final failure details.

## Epic 9: AI Integration

### TASK-080: Define AI provider abstraction

Create `IAiAnalysisProvider`.

### TASK-081: Implement Claude provider

Call Claude API using configuration.

### TASK-082: Build prompt v1

Implement the first English prompt version based on `05_AI_PIPELINE.md`.

### TASK-083: Validate AI response schema

Ensure the AI response can be parsed into the report schema.

### TASK-084: Save AI request metadata

Persist AI request status and usage metadata.

Do not store full prompt bodies or raw AI input/output by default. Add only a development-only debug option if explicitly needed.

### TASK-085: Save report

Persist structured report in `car_reports`.

## Epic 10: Payments

Payments should start after the free-check lifecycle works end to end.

### TASK-090: Configure Stripe Checkout

Create endpoint to start checkout for one check or five-check package.

### TASK-091: Implement Stripe webhook endpoint

Verify webhook signature and process events.

### TASK-092: Grant credits after successful payment

Use webhook as source of truth.

### TASK-093: Ensure webhook idempotency

Store processed Stripe event ids.

## Epic 11: Frontend

### TASK-100: Create Next.js app

Set up frontend application.

### TASK-101: Implement basic layout

Create landing page and authenticated dashboard shell.

### TASK-102: Implement sign-in UI

Connect to authentication flow.

### TASK-103: Implement check creation form

Support listing URL, text, optional metadata, notes, and screenshot upload.

### TASK-104: Implement credit balance display

Show available credits.

### TASK-105: Implement report history

Show previous checks and statuses.

### TASK-106: Implement report view

Render structured AI report.

### TASK-107: Implement payment UI

Allow buying one check or a five-check package.

## Epic 12: Deployment

### TASK-110: Add production Docker Compose override

Configure production services and volumes.

### TASK-111: Configure Nginx

Add reverse proxy config.

### TASK-112: Add backup notes/scripts

Add initial scripts or documentation for PostgreSQL and SeaweedFS backup.

### TASK-113: Add deployment documentation

Document local and production startup.

## Epic 13: Hardening

### TASK-120: Add authorization tests

Ensure users cannot access other users' checks or reports.

### TASK-121: Add upload security tests

Validate file upload restrictions.

### TASK-122: Add processing idempotency tests

Ensure duplicate messages do not create duplicate reports.

### TASK-123: Add payment idempotency tests

Ensure Stripe events are processed once.

### TASK-124: Add basic observability

Add logs for check lifecycle and processing pipeline.

### TASK-125: Add minimal protected admin operations

Add protected internal/admin operations for failed check inspection, retry, manual refund, user credit balance inspection, and processing status inspection.
