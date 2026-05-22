# Non-Functional Requirements

## 1. Reliability

### NFR-REL-001: No Lost Analysis Tasks

If the API successfully creates a car check, the corresponding analysis command must not be lost.

The system must use the transactional outbox pattern.

### NFR-REL-002: Durable Messaging

NATS JetStream must use persistent streams and durable consumers.

### NFR-REL-003: At-Least-Once Delivery

The system must be designed for at-least-once message delivery.

ProcessingService must be idempotent.

### NFR-REL-004: Retry Policy

ProcessingService must support retries for temporary failures:

- temporary Claude API failure;
- temporary SeaweedFS failure;
- temporary PostgreSQL failure;
- temporary network issue.

### NFR-REL-005: Failed State

After retry exhaustion, the check must move to `Failed`.

### NFR-REL-006: Credit Safety

Credits must not be consumed twice for the same check.

Technical failures should allow credit refund.

Credit consumption, check creation, file metadata persistence, and outbox message creation must be committed atomically in one PostgreSQL transaction.

Automatic refunds are limited to clearly classified technical failures. Low-confidence analysis or poor user input must not trigger an automatic refund when a report is generated successfully.

## 2. Performance

### NFR-PERF-001: API Responsiveness

The API must not block while AI analysis is running.

After check creation, the API must return quickly with a `checkId` and status.

### NFR-PERF-002: Asynchronous Processing

AI analysis must be performed by ProcessingService, not by the API service.

### NFR-PERF-003: Initial Scale

MVP should support a small number of concurrent users and analysis jobs on a single VPS.

No premature horizontal scaling is required.

## 3. Security

### NFR-SEC-001: Authentication Required

All private endpoints must require authentication.

Authentication must be backend-owned. The API Service owns Google OAuth, local user creation, backend-issued auth cookies or JWTs, and authorization. Frontend-owned auth systems must not be authoritative.

### NFR-SEC-002: Authorization

Users must only access resources they own.

### NFR-SEC-003: Secrets Management

API keys and secrets must not be committed to Git.

Secrets must be provided through environment variables or deployment secrets.

### NFR-SEC-004: File Upload Safety

The system must validate uploaded files:

- allowed content types;
- maximum file size;
- maximum number of files per check.

MVP file uploads must be API-mediated. The browser must not upload directly to object storage using presigned URLs in MVP.

### NFR-SEC-005: Stripe Webhook Verification

Stripe webhook signatures must be verified.

### NFR-SEC-006: No Public Storage Access by Default

Uploaded files must not be publicly accessible by default.

## 4. Privacy and Data Protection

### NFR-PRIV-001: Minimal Data Collection

The system must collect only data required for product functionality.

### NFR-PRIV-002: User Data Isolation

User data must be isolated at application level through ownership checks.

### NFR-PRIV-003: Deletion Strategy

The system should support deletion of user checks and associated files.

### NFR-PRIV-004: AI Input Awareness

The system must assume that submitted data may be sent to the configured AI provider for analysis.

This must be reflected in user-facing legal/privacy text before public launch.

### NFR-PRIV-005: AI Prompt and Raw Response Storage

The system must not store full AI prompt bodies, raw AI input, or raw AI output by default.

The system may support a development-only option to store raw prompt/input/output for debugging. This option must be disabled by default and must not be enabled in production unless explicitly configured.

## 5. Maintainability

### NFR-MAINT-001: Monorepo

All services must live in one GitHub repository.

### NFR-MAINT-002: Clear Service Boundaries

The API service and ProcessingService must have clear responsibilities.

### NFR-MAINT-003: Provider Abstractions

The system must abstract:

- AI provider;
- object storage;
- message publishing/consumption where practical.

### NFR-MAINT-004: FluentValidation Only

Backend request validation must be implemented with FluentValidation only.

### NFR-MAINT-005: English Documentation

All project documentation must be written in English.

## 6. Observability

### NFR-OBS-001: Structured Logs

Services must use structured logging.

### NFR-OBS-002: Correlation IDs

Requests and background processing should include correlation IDs.

### NFR-OBS-003: Processing Traceability

The system must make it possible to trace:

- check creation;
- outbox publication;
- message consumption;
- AI request;
- report creation;
- failure reason.

### NFR-OBS-004: Health Checks

Services should expose health checks for local and production deployment.

## 7. Deployment

### NFR-DEPLOY-001: Docker Compose

The system must be runnable with Docker Compose both locally and on the production VPS.

Next.js must run as a separate Node service behind nginx. Static export is not part of the MVP deployment model.

### NFR-DEPLOY-002: Ubuntu VPS

Production target is Ubuntu Server 24.04.

### NFR-DEPLOY-003: GitHub Source Control

The source code must be stored in GitHub.

### NFR-DEPLOY-004: Environment Configuration

Environment-specific configuration must be externalized.

## 8. Cost Control

### NFR-COST-001: AI Cost Awareness

AI usage must be tracked where possible.

### NFR-COST-002: Processing Limits

The system should limit input size and uploaded file count to prevent excessive AI costs.

### NFR-COST-003: No Unnecessary Infrastructure

MVP should avoid heavy infrastructure that increases VPS resource requirements without clear value.
