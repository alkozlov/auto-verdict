# Observability

AutoVerdict uses OpenTelemetry for application metrics and a local Grafana stack for visual monitoring.

## Local Stack

Docker Compose starts:

- `otel-collector` — receives OTLP metrics from services.
- `prometheus` — scrapes metrics exposed by the collector.
- `grafana` — visualizes Prometheus data.

Local endpoints:

```txt
OpenTelemetry OTLP gRPC: http://localhost:4317
OpenTelemetry OTLP HTTP: http://localhost:4318
Prometheus:              http://localhost:9090
Grafana:                 http://localhost:3001
```

Default Grafana credentials are configured through:

```txt
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=admin
```

The default Prometheus datasource and `AI Token Usage` dashboard are provisioned automatically.

## ProcessingService AI Metrics

ProcessingService exports AI pipeline metrics through OpenTelemetry.

Current custom meter:

```txt
AutoVerdict.ProcessingService.AI
```

Metrics:

| Metric | Type | Unit | Purpose |
|--------|------|------|---------|
| `autoverdict_ai_tokens_total` | Counter | tokens | Token usage by provider, model, stage, request status, and token type |
| `autoverdict_ai_estimated_cost_eur_total` | Counter | EUR | Estimated AI cost by provider, model, stage, and request status |
| `autoverdict_ai_requests_total` | Counter | requests | AI request count by provider, model, stage, and status |
| `autoverdict_ai_request_duration_ms` | Histogram | ms | AI request latency by provider, model, stage, and status |

Labels:

```txt
ai.provider
ai.model
ai.stage
ai.request.status
ai.token.type
```

`ai.token.type` is emitted for `input`, `output`, and `total` token counters.

Per-check identifiers are intentionally not metric labels because they would create high-cardinality time series. Per-check AI run details remain in PostgreSQL `ai_runs`.

## Example PromQL

Token rate by model:

```promql
sum(rate(autoverdict_ai_tokens_total{ai_token_type="total"}[5m])) by (ai_model)
```

Estimated cost over the dashboard range:

```promql
sum(increase(autoverdict_ai_estimated_cost_eur_total[$__range])) by (ai_model)
```

Tokens by model and pipeline stage:

```promql
sum(increase(autoverdict_ai_tokens_total[$__range])) by (ai_model, ai_stage, ai_token_type)
```
