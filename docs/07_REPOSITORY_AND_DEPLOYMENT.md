# Repository and Deployment

## 1. Repository Strategy

The project uses a single GitHub repository.

Even though the architecture contains multiple services, all source code lives in one repository. This simplifies:

- local development;
- Docker Compose;
- shared contracts;
- versioning;
- AI-assisted development;
- deployment to a single VPS.

## 2. Suggested Monorepo Structure

```txt
auto-verdict/
  README.md
  AGENTS.md
  .gitignore
  .editorconfig
  .env.example
  docker-compose.yml
  docker-compose.override.yml
  docker-compose.prod.yml

  docs/
    01_PRD.md
    02_FUNCTIONAL_REQUIREMENTS.md
    03_NON_FUNCTIONAL_REQUIREMENTS.md
    04_ARCHITECTURE.md
    05_AI_PIPELINE.md
    06_DATA_MODEL.md
    07_REPOSITORY_AND_DEPLOYMENT.md
    08_BACKLOG.md
    09_BEADS_WORKFLOW.md

  src/
    frontend/
    backend/
      AutoVerdict.sln
      src/
        AutoVerdict.Api/
        AutoVerdict.ProcessingService/
        AutoVerdict.Contracts/
        AutoVerdict.Domain/
        AutoVerdict.Application/
        AutoVerdict.Infrastructure/
      tests/
        AutoVerdict.Api.Tests/
        AutoVerdict.Application.Tests/
        AutoVerdict.ProcessingService.Tests/

  infra/
    nginx/
    nats/
    seaweedfs/
    postgres/

  scripts/
```

The exact namespace can be changed later.

## 3. Backend Projects

### AutoVerdict.Api

ASP.NET Core Minimal API.

Responsibilities:

- HTTP endpoints;
- backend-owned Google OAuth authentication;
- backend-issued auth cookie or JWT;
- API authorization;
- FluentValidation;
- car check creation;
- payment endpoints;
- Stripe webhooks;
- file upload orchestration;
- outbox publishing loop.

The API is authoritative for authentication, users, credits, payments, and authorization. The frontend must not use NextAuth/Auth.js as the authoritative authentication system.

### AutoVerdict.ProcessingService

.NET Worker Service.

Responsibilities:

- NATS JetStream consumer;
- AI analysis pipeline;
- report persistence;
- processing failure handling.

### AutoVerdict.Contracts

Shared contracts:

- message contracts;
- DTOs shared between services;
- report schemas.

### AutoVerdict.Domain

Domain entities and enums.

### AutoVerdict.Application

Application services and use cases.

### AutoVerdict.Infrastructure

Infrastructure integrations:

- PostgreSQL;
- EF Core;
- NATS;
- SeaweedFS/S3 client;
- Claude API;
- Stripe.

## 4. Docker Compose Services

Minimum services:

```txt
frontend
api
processing-service
postgres
nats
seaweedfs
nginx
```

Local development may skip nginx and access frontend/API directly.

The frontend service is a Node-based Next.js container. Static export is not used for MVP.

## 5. Local Development

Example command:

```bash
docker compose up --build
```

Expected local endpoints:

```txt
Frontend: http://localhost:3000
API:      http://localhost:5000
NATS:     localhost:4222
SeaweedFS S3: http://localhost:8333
Postgres: localhost:5432
```

Actual ports may be adjusted.

## 6. Production Deployment

Target environment:

- Ubuntu Server 24.04 VPS;
- Docker;
- Docker Compose;
- GitHub repository;
- Nginx reverse proxy;
- HTTPS through reverse proxy/certbot or another TLS setup.

Production deployment should use:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

## 7. Environment Variables

Configuration must be externalized.

Example categories:

```txt
DATABASE_URL
NATS_URL
S3_ENDPOINT
S3_ACCESS_KEY
S3_SECRET_KEY
S3_BUCKET
CLAUDE_API_KEY
CLAUDE_MODEL
STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
AUTH_SECRET
```

No secrets may be committed to Git.

## 8. Volumes

Persistent services require volumes:

```txt
postgres_data
nats_data
seaweedfs_data
```

Backups must include at least:

- PostgreSQL database;
- SeaweedFS object data;
- NATS JetStream data if unpublished/unprocessed messages must survive disaster recovery.

## 9. GitHub

The source code is stored in GitHub.

GitHub is used for remote Git hosting, pull requests if needed, code review, and project history.

## 10. Branching Strategy

Suggested simple strategy:

- `main` — stable branch;
- short-lived feature branches.

Example:

```txt
feature/google-auth
fix/stripe-webhook-idempotency
```

## 11. Pull Request Rules

Every non-trivial change should go through a pull request.

PR description should include:

- what changed;
- how it was tested;
- migration notes, if any;
- configuration changes, if any.

## 12. Task Management

Documentation files provide product context, requirements, and architectural constraints.

Beads is the operational task management system. See `09_BEADS_WORKFLOW.md`.

## 13. Deployment Notes

MVP deployment is intentionally simple. Avoid Kubernetes, Terraform, complex service discovery, and managed queues unless the project outgrows a single VPS.

The immediate goal is a reliable, understandable, easily reproducible Docker Compose deployment.
