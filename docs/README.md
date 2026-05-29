# Auto Verdict / AI Used Car Listing Checker

A SaaS application for preliminary AI-assisted analysis of used car listings.

The product helps inexperienced private buyers identify potential risks, inconsistencies, missing information, suspicious wording, model-specific issues, and practical questions to ask before contacting a seller or visiting a car.

The product does **not** replace a professional vehicle inspection, legal verification, official vehicle history report, or mechanic. It is a decision-support tool for early-stage screening.

## MVP Market

- Initial market: Poland.
- Initial listing source: Otomoto.pl.
- Target users: private buyers looking for relatively recent used cars, approximately model year 2022 or newer.
- Initial input model: user-provided listing URL, listing text, screenshots, and optional official/public vehicle history data.
- Future direction: integrations with official and public vehicle history sources across Europe.

## Core Idea

A user creates a car check, provides listing data, uploads screenshots, and optionally adds VIN, registration number, first registration date, or official vehicle history data. The system analyzes the input using AI and returns a structured risk report.

The report includes:

- overall risk level;
- detected inconsistencies;
- missing information;
- model-specific risks;
- questions to ask the seller;
- inspection checklist;
- final recommendation.

## Accepted Technology Stack

### Frontend

- Next.js
- TypeScript
- Tailwind CSS
- shadcn/ui

### Backend

- .NET 10
- C#
- ASP.NET Core Minimal API
- FluentValidation only for backend validation
- PostgreSQL
- EF Core
- NATS JetStream
- SeaweedFS via S3-compatible API
- Claude API as the first AI provider

### Services

- `frontend` — user interface.
- `api` — HTTP API, authentication, validation, users, credits, payments, uploads, task submission.
- `processing-service` — asynchronous car check processing, Claude calls, report generation.
- `postgres` — shared relational database.
- `nats` — message bus with JetStream persistence.
- `seaweedfs` — S3-compatible object storage.
- `otel-collector` — OpenTelemetry metrics receiver and VictoriaMetrics writer.
- `victoria-metrics` — local metrics time-series store.
- `grafana` — local metrics dashboards.
- `nginx` — production reverse proxy.
- `docker-compose` — single local and production orchestration entry point.

## Documentation

- [01_PRD.md](./01_PRD.md)
- [02_FUNCTIONAL_REQUIREMENTS.md](./02_FUNCTIONAL_REQUIREMENTS.md)
- [03_NON_FUNCTIONAL_REQUIREMENTS.md](./03_NON_FUNCTIONAL_REQUIREMENTS.md)
- [04_ARCHITECTURE.md](./04_ARCHITECTURE.md)
- [05_AI_PIPELINE.md](./05_AI_PIPELINE.md)
- [06_DATA_MODEL.md](./06_DATA_MODEL.md)
- [07_REPOSITORY_AND_DEPLOYMENT.md](./07_REPOSITORY_AND_DEPLOYMENT.md)
- [08_BACKLOG.md](./08_BACKLOG.md)
- [09_BEADS_WORKFLOW.md](./09_BEADS_WORKFLOW.md)
- [10_PROCESSING_DEBUG.md](./10_PROCESSING_DEBUG.md)
- [12_OBSERVABILITY.md](./12_OBSERVABILITY.md)
