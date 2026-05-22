# Functional Requirements

## 1. Authentication and User Management

### FR-AUTH-001: Google Sign-In

The system must allow users to register and sign in using Google.

Google OAuth must be owned by the API Service. The frontend must not use NextAuth/Auth.js or another frontend-owned auth system as the authoritative authentication system.

The API Service must:

- handle Google OAuth login;
- create or update the local user record;
- issue the backend-owned auth cookie or JWT;
- authorize API requests using that backend-issued authentication.

### FR-AUTH-002: User Profile

The system must store a user profile with:

- user id;
- email;
- display name;
- avatar URL, if available;
- registration date;
- last login date.

### FR-AUTH-003: Authenticated Access

Creating checks, viewing reports, uploading files, and buying credits must be available only to authenticated users.

## 2. Credits and Free Checks

### FR-CREDITS-001: Initial Free Checks

After registration, the user must receive an initial configurable number of free checks.

```txt
InitialFreeChecks = 1 or 2
```

### FR-CREDITS-002: Available Balance

The system must show the user the number of available check credits.

### FR-CREDITS-003: Credit Consumption

Starting an analysis must consume one credit.

Credit consumption must happen in the same PostgreSQL transaction as:

- car check creation;
- initial input persistence;
- uploaded file metadata persistence;
- outbox message insertion.

The system must not create a queued check without consuming a credit, and must not consume a credit without creating the check and outbox message.

### FR-CREDITS-004: Refund on Technical Failure

If processing fails because of a technical/system error, the system should be able to refund the consumed credit.

MVP must automatically refund only clearly classified technical failures, such as:

- AI provider unavailable;
- ProcessingService internal exception;
- storage read failure;
- invalid AI JSON response after retries;
- message processing infrastructure failure.

The system must not automatically refund for poor user input, missing listing data, or completed reports with low confidence.

The system must also provide a protected manual refund operation for internal/admin use.

### FR-CREDITS-005: No Negative Balance

The system must never allow the user's credit balance to become negative.

## 3. Payments

### FR-PAY-001: Payment Options

MVP must support exactly two paid options:

- one check;
- package of five checks.

### FR-PAY-002: Stripe Checkout

The system must use Stripe Checkout for payment initiation.

### FR-PAY-003: Stripe Webhooks

The system must use Stripe webhooks as authoritative confirmation for successful payments.

### FR-PAY-004: Credit Granting

After a successful webhook event:

- single check purchase grants 1 credit;
- package purchase grants 5 credits.

### FR-PAY-005: Idempotent Webhook Handling

The system must process Stripe events idempotently and must not grant credits twice for the same event.

Stripe implementation should follow the completed free-check lifecycle. The free-check flow is the first implementation priority.

## 4. Car Check Creation

### FR-CHECK-001: Create Check

Authenticated users must be able to create a new car check.

### FR-CHECK-002: Required Input

At least one meaningful source of listing data must be provided:

- listing URL;
- listing text;
- screenshot upload.

Exact validation rules must be implemented with FluentValidation.

### FR-CHECK-003: Optional Input

The user may provide:

- VIN;
- registration number;
- first registration date;
- price;
- seller name or type;
- pasted vehicle history data;
- additional notes.

### FR-CHECK-004: Screenshot Upload

The user must be able to upload one or more screenshots related to a listing.

MVP uploads must be API-mediated. The browser uploads files to the API Service; the API validates them, stores them in SeaweedFS through the storage abstraction, and writes metadata to PostgreSQL.

Presigned direct browser uploads are out of scope for MVP.

### FR-CHECK-005: Input Storage

The system must store submitted text data in PostgreSQL and uploaded files in SeaweedFS.

### FR-CHECK-006: Check Status

A check must have a status.

Minimum statuses:

```txt
Queued
Processing
Completed
Failed
Cancelled
```

Optional future status:

```txt
PendingPayment
```

### FR-CHECK-007: User Ownership

Users must only access their own checks and reports.

## 5. Message Submission

### FR-MSG-001: Queue Analysis

After a valid check is created and a credit is consumed, the API must submit an analysis command through the message bus.

### FR-MSG-002: NATS JetStream

The system must use NATS JetStream for durable message persistence.

### FR-MSG-003: Transactional Outbox

The API must use a transactional outbox pattern to avoid losing analysis commands when database writes succeed but message publishing fails.

## 6. Processing Service

### FR-PROC-001: Consume Analysis Commands

ProcessingService must consume car check analysis commands from NATS JetStream.

### FR-PROC-002: Load Check Data

ProcessingService must load all required check data from PostgreSQL.

### FR-PROC-003: Load Files

ProcessingService must load uploaded screenshots from SeaweedFS.

### FR-PROC-004: Call AI Provider

ProcessingService must call the configured AI provider through an internal abstraction.

### FR-PROC-005: Claude as First Provider

MVP must implement Claude API as the first AI provider.

### FR-PROC-006: Save AI Request Metadata

The system should store metadata about AI requests:

- provider;
- model;
- request time;
- response time;
- token usage, if available;
- status;
- error message, if any.

### FR-PROC-007: Save Report

ProcessingService must save the structured report in PostgreSQL.

### FR-PROC-008: Update Status

ProcessingService must update check status to `Completed` or `Failed`.

## 7. AI Report

### FR-REPORT-001: Structured Report

The report must be structured and machine-readable.

### FR-REPORT-002: Report Sections

The report must include:

- risk level;
- confidence level;
- summary;
- extracted vehicle facts;
- positive signals;
- risk signals;
- missing information;
- questions to ask the seller;
- inspection checklist;
- model-specific risks;
- final recommendation;
- disclaimer.

### FR-REPORT-003: Risk Signal Severity

Each risk signal must include severity:

```txt
low
medium
high
```

### FR-REPORT-004: Recommendation

Final recommendation must be one of:

```txt
proceed
proceed_with_caution
request_more_info
avoid
```

### FR-REPORT-005: Cautious Language

The report must avoid unsupported absolute claims.

### FR-REPORT-006: Report Language

MVP reports must be generated in English.

### FR-REPORT-007: Source Distinction

The report must distinguish between:

- facts extracted from the listing;
- facts provided by the user;
- possible model-specific risks;
- recommendations and questions.

Model-specific risks may rely on Claude's general knowledge for MVP, but must use cautious wording and explicit uncertainty unless supported by provided data.

## 8. Report History

### FR-HISTORY-001: List Checks

Users must be able to view their previous checks.

### FR-HISTORY-002: Open Report

Users must be able to open a completed report.

### FR-HISTORY-003: Failed Checks

Users must be able to see when a check failed.

## 9. File Storage

### FR-STORAGE-001: SeaweedFS

The MVP must use SeaweedFS as S3-compatible object storage.

### FR-STORAGE-002: Storage Abstraction

The backend must use an internal storage abstraction and must not depend on SeaweedFS-specific APIs.

### FR-STORAGE-003: File Metadata

File metadata must be stored in PostgreSQL.

### FR-STORAGE-004: File Cleanup

The system must provide a strategy for deleting unused or orphaned files.

## 10. Admin / Operations

### FR-OPS-001: Health Checks

Each service must expose health endpoints where applicable.

### FR-OPS-002: Logging

Services must produce structured logs.

### FR-OPS-003: Configuration

All secrets and environment-specific values must be provided through environment variables.

### FR-OPS-004: Minimal Internal Admin Operations

MVP must provide minimal protected internal/admin operations for:

- viewing failed checks;
- retrying failed checks;
- refunding one credit manually;
- inspecting a user's credit balance;
- inspecting processing status.

These operations must not be public endpoints and must require explicit admin authorization/configuration.
