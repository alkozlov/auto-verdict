# Architecture

## 1. Architecture Style

The project moves toward a microservice-oriented architecture, but the MVP uses a pragmatic service-based monorepo:

- multiple separate services;
- one GitHub repository;
- one shared PostgreSQL database;
- one Docker Compose setup;
- clear service responsibilities;
- asynchronous processing via message bus.

This gives separation of concerns without operational complexity of a full distributed microservices platform.

## 2. High-Level Components

```txt
                 ┌────────────────────┐
                 │      Frontend      │
                 │ Vite + React + TS  │
                 └─────────┬──────────┘
                           │ HTTP
                           ▼
                 ┌────────────────────┐
                 │    API Service     │
                 │ .NET 10 Minimal API│
                 └──────┬──────┬──────┘
                        │      │
              SQL       │      │ S3-compatible API
                        │      │
                        ▼      ▼
              ┌────────────┐  ┌──────────────┐
              │ PostgreSQL │  │  SeaweedFS   │
              └────────────┘  └──────────────┘
                        ▲
                        │ SQL
                        │
                 ┌──────┴─────────────┐
                 │  ProcessingService │
                 │ .NET Worker        │
                 └──────┬─────────────┘
                        │
                        │ Claude API
                        ▼
                 ┌──────────────┐
                 │ Claude API   │
                 └──────────────┘

                 ┌──────────────┐
                 │ NATS         │
                 │ JetStream    │
                 └──────────────┘
```

## 3. Services

### 3.1 Frontend

Technology:

- Vite;
- React;
- React Router;
- TypeScript;
- Tailwind CSS;
- lucide-react icons.

The frontend is a static single-page application. Vite builds static assets into `src/frontend/dist`, and nginx serves them with an SPA fallback to `index.html`.

Responsibilities:

- public marketing pages;
- sign-in flow;
- user dashboard;
- check creation form;
- screenshot upload UI;
- credit balance display;
- payment initiation UI;
- check status display;
- report display.

Implemented public routes:

```txt
/
/how-it-works
/sample-report
/pricing
/privacy
/terms
/contact
```

Implemented authenticated routes:

```txt
/garage/check
/garage/reports
/garage/reports/:id
/auth/callback
```

The frontend uses `react-i18next`/`i18next` for interface language selection. Supported UI/report locales are:

```txt
en, pl, de, uk, fr
```

The selected locale is stored in browser local storage and is sent as `reportLocale` when creating a car check.

Public pages set route-specific title, meta description, canonical URL, Open Graph tags, Twitter Card tags, and JSON-LD client-side. This is an MVP-level SEO implementation only; public pages should later be statically prerendered, split into an SSR/SSG marketing site, or moved to an SSR/SSG framework.

Frontend must not contain business-critical payment or credit logic.

Frontend must not own authentication. It calls backend auth endpoints and uses the backend-issued JWT.

### 3.2 API Service

Technology:

- .NET 10;
- ASP.NET Core Minimal API;
- C#;
- FluentValidation;
- EF Core;
- PostgreSQL;
- NATS JetStream client;
- S3-compatible storage client.

Responsibilities:

- HTTP API;
- backend-owned Google OAuth authentication;
- local user creation and update;
- backend-issued auth cookie or JWT;
- API authorization;
- user profile management;
- credit balance management;
- payment initiation;
- Stripe webhook processing;
- file upload orchestration;
- input validation using FluentValidation;
- car check creation;
- outbox message creation;
- check status and report retrieval.

The API service must not perform long-running AI analysis.

The API handles MVP uploads directly: the browser uploads screenshots to the API, the API validates them, stores them in SeaweedFS through the storage abstraction, and persists metadata in PostgreSQL.

### 3.3 ProcessingService

Technology:

- .NET 10 Worker Service;
- C#;
- PostgreSQL;
- NATS JetStream client;
- S3-compatible storage client;
- Claude API client.

Responsibilities:

- consume analysis commands from NATS JetStream;
- load check data from PostgreSQL;
- load uploaded files from SeaweedFS;
- build AI request;
- call Claude API via provider abstraction;
- validate AI response;
- save report;
- update check status;
- handle retries and failures;
- maintain idempotency.

## 4. Shared Database

MVP uses one PostgreSQL database shared by API and ProcessingService.

This is acceptable for MVP, but table ownership must be clear.

### API-Owned Areas

- users;
- external auth accounts;
- credits;
- payments;
- Stripe events;
- initial car check creation;
- uploaded file metadata;
- outbox messages.

### ProcessingService-Owned Areas

- processing status transitions;
- AI request metadata;
- car reports;
- processing errors;
- inbox/idempotency records.

## 5. Message Bus

The system uses NATS JetStream.

Reasons:

- lightweight resource usage for VPS deployment;
- persistent streams;
- durable consumers;
- suitable for asynchronous job processing;
- Apache-2.0 license;
- simple Docker deployment.

### Stream

```txt
Stream: CAR_CHECKS
Subjects:
- car-check.analysis.requested
```

### Message Contract

```json
{
  "checkId": "uuid",
  "userId": "uuid",
  "description": "copied listing text, seller messages, VIN details, or notes",
  "listingUrl": "https://www.otomoto.pl/...",
  "requestedAt": "2026-05-22T12:00:00Z",
  "reportLocale": "en",
  "userImageKeys": ["check-id/user-images/image-1.jpg"]
}
```

## 6. Transactional Outbox

The API must not rely only on direct message publishing after database commit.

When creating a check, API writes both the check and an outbox message in the same PostgreSQL transaction.

```txt
BEGIN
  INSERT car_checks
  INSERT uploaded file metadata when applicable
  INSERT outbox_messages
COMMIT
```

A publisher loop reads unpublished outbox messages and publishes them to NATS JetStream.

This prevents losing analysis commands if PostgreSQL succeeds but NATS publishing fails.

The transaction must not create a queued check without ensuring the user has an available credit or whitelist access. Credit consumption is finalized on successful report completion.

## 7. Inbox / Idempotency

ProcessingService must assume at-least-once delivery.

Before processing a message, it must ensure the message was not already processed.

Possible strategy:

- store consumed message id in `inbox_messages`;
- check current car check status;
- if report already exists, acknowledge and skip.

## 8. Storage

The system uses SeaweedFS as S3-compatible object storage.

The application must access storage through an abstraction:

```csharp
public interface IObjectStorage
{
    Task<StoredObject> PutAsync(ObjectUpload upload, CancellationToken cancellationToken);
    Task<Stream> GetAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
```

The implementation must use generic S3-compatible operations, not SeaweedFS-specific APIs.

## 9. AI Provider Abstraction

Business logic must not depend directly on Claude.

```csharp
public interface IAiAnalysisProvider
{
    Task<CarAnalysisResult> AnalyzeAsync(CarAnalysisRequest request, CancellationToken cancellationToken);
}
```

MVP implementation:

```txt
ClaudeAiAnalysisProvider
```

Future implementations may include OpenAI, Gemini, or another provider.

## 10. Check Processing Flow

```txt
1. User submits check form.
2. API validates input with FluentValidation.
3. API validates and stores user images in SeaweedFS through the storage abstraction.
4. API starts a PostgreSQL transaction.
5. API checks that the user has one available credit or whitelist access.
6. API writes car check, uploaded file metadata as needed, and an outbox message in the same transaction.
7. API commits the transaction.
8. API returns checkId and status Queued.
9. Outbox publisher publishes message to NATS JetStream.
10. ProcessingService consumes message.
11. ProcessingService downloads uploaded files and crawls supported listing URLs when available.
12. ProcessingService runs staged AI analysis and report generation.
13. ProcessingService validates the localized markdown report.
14. ProcessingService saves report.
15. ProcessingService marks check as Completed and consumes one credit.
```

## 11. Failure Flow

```txt
1. ProcessingService receives message.
2. Processing starts.
3. Temporary error happens.
4. ProcessingService retries according to policy.
5. If retries are exhausted, check is marked Failed.
6. Automatic credit refund is applied only for clearly classified technical failures.
7. Failure details are stored for diagnostics.
```

Low-confidence successful reports and poor user input do not trigger automatic refunds.

## 12. Deployment Topology

MVP runs on a single VPS with Docker Compose.

Services:

```txt
frontend
api
processing-service
postgres
nats
seaweedfs
nginx
```

Local and production deployments share the same basic structure, with different environment variables and compose overrides.

The frontend service is an nginx/static-assets container serving the Vite build output. Nginx proxies `/api/*` to the API service and serves all other routes from the SPA with fallback to `index.html`.

## 13. Minimal Internal Admin Operations

MVP does not include a full admin panel.

The API should provide protected internal/admin operations or CLI commands for:

- viewing failed checks;
- retrying failed checks;
- refunding one credit manually;
- inspecting a user's credit balance;
- inspecting processing status.

These operations must require explicit admin authorization/configuration and must not be public endpoints.

## 14. Architecture Decisions

### ADR-001: Use service-based monorepo

Decision: all services live in one GitHub repository.

Reason: simpler local development, easier AI-assisted development, shared contracts, and simpler Docker Compose deployment.

### ADR-002: Use NATS JetStream

Decision: use NATS JetStream instead of RabbitMQ for MVP.

Reason: lower resource footprint, persistence support, durable consumers, and sufficient feature set for asynchronous AI jobs.

### ADR-003: Avoid MassTransit

Decision: do not use MassTransit as a core dependency.

Reason: licensing uncertainty for newer versions and unnecessary abstraction for MVP.

### ADR-004: Use SeaweedFS

Decision: use SeaweedFS as self-hosted S3-compatible object storage.

Reason: better object storage model than raw filesystem, Apache-2.0 license, and future compatibility with S3-like providers.

### ADR-005: Use Claude first, but abstract AI provider

Decision: implement Claude API first, behind `IAiAnalysisProvider`.

Reason: allows provider replacement without rewriting business logic.

### ADR-006: Backend-owned authentication

Decision: the API Service owns Google OAuth, local users, backend-issued session/token creation, and API authorization.

Reason: user identity, credits, payments, and authorization stay under backend control.

### ADR-007: API-mediated uploads for MVP

Decision: screenshots are uploaded to the API, not directly from the browser to object storage.

Reason: simpler validation, authorization, metadata persistence, and local development for MVP.

### ADR-008: Vite React SPA frontend

Decision: run the frontend as a Vite-built React SPA served by nginx.

Reason: the current MVP frontend is primarily client-side, simple to deploy as static assets, and sufficient for authenticated dashboard flows.

Consequence: public-page SEO metadata is currently injected client-side. Before serious SEO work, add static prerendering, split public marketing pages into an SSR/SSG site, or move public pages to an SSR/SSG-compatible framework.
