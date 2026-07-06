# Auto-Verdict → vps-gateway Infra Migration — Design

**Date:** 2026-07-06 · **Status:** Approved

## Problem

auto-verdict runs its own postgres, NATS, and SeaweedFS on the VPS alongside the shared vps-gateway stack (which already hosts postgres and SeaweedFS for fartoff). This duplicates services on one machine, and auto-verdict's production SeaweedFS accepts credentials (`dev-access-key`/`dev-secret-key`) committed to its public repo. Nothing on the VPS is backed up.

## Decisions (made with user)

- **Approach: copy-and-flip.** pg_dump/restore + `mc mirror`, then flip env vars. No volume transplants (gateway postgres already holds fartoff's cluster; SeaweedFS would jump 3.85→4.37 on live data).
- **Migrate all 21 GB** of SeaweedFS data (raw evidence kept; disk has 111 GB free).
- **Prep + cutover in one session** (~5–10 min API downtime at the flip).
- **Nightly postgres backups added to the gateway now** (roadmap item folded in).
- Loki/promtail/grafana **stay in auto-verdict** (per user, 2026-07-03).
- `deploy-infra.yml` **deleted** after migration (per user, 2026-07-03).

## Facts (verified on VPS 2026-07-06)

- Both postgres are 17.10; auto-verdict DB = 8.4 MB.
- auto-verdict SeaweedFS `/data` = 21 GB (v3.85); gateway SeaweedFS ~empty (v4.37 via unpinned `:latest`).
- Gateway has no NATS. auto-verdict's stream `AUTOVERDICT_CHECKS` is a 24 h-retention work queue — config-only move, no data.

## vps-gateway repo changes

- `postgres`: keep name + legacy `postgres` alias (fartoff depends on it); add alias **`gateway-postgres`** (new cross-project convention). Adding the alias recreates the container → ~10 s fartoff DB blip, accepted.
- `seaweedfs`: pin image **`chrislusf/seaweedfs:4.37`** (what `:latest` already runs); add alias **`gateway-seaweedfs`**, keep legacy `seaweedfs`.
- New **`gateway-nats`**: `nats:2.10-alpine` (matches auto-verdict's current version), JetStream (`store_dir /data`, 1 GB mem / 10 GB file limits — same as auto-verdict's nats.conf), volume `gateway-nats-data`, `http_port 8222` healthcheck, on `caddy-gateway`. No client auth (internal-network trust, same as postgres today).
- New **`postgres-backup`** sidecar: `postgres:17` image, shell loop — nightly `pg_dumpall -h postgres` (all DBs incl. fartoff) gzipped to volume `postgres-backups`, delete files older than 7 days. Credentials via existing `POSTGRES_FARTOFF_*` env (cluster superuser).
- Secret `SEAWEEDFS_S3_CONFIG`: add `autoverdict` identity, freshly generated keys, actions scoped to bucket `auto-verdict-local` (`Read:auto-verdict-local`, `Write:...`, `List:...`, `Tagging:...` — no Admin). fartoff identity untouched.

## auto-verdict repo changes

- Move `postgres`, `nats`, `nats-setup`, `seaweedfs`, `seaweedfs-setup` (+ their volume declarations and the api/processing `depends_on` entries) from `docker-compose.yml` into `docker-compose.override.yml`. Local dev (`podman compose up`, override auto-loaded) keeps the full stack; prod (explicit `-f docker-compose.yml -f docker-compose.prod.yml`) no longer runs them. `infra/nats/*` and `infra/seaweedfs/s3.json` (dev creds) remain in-repo for local dev only.
- Rename `processing-service` → **`auto-verdict-processing`** and attach it to `caddy-gateway` (it must reach gateway services; prefix convention for that network). Update `docker-compose.prod.yml` service key accordingly.
- Grafana postgres datasource (`infra/grafana/provisioning/datasources/postgres.yml`): `url: gateway-postgres:5432`, `user: autoverdict`, password via `$__env{GRAFANA_DB_PASSWORD}` (compose passes it from `.env`); no committed credentials.
- Delete `.github/workflows/deploy-infra.yml`.
- `docker-compose.prod.yml`: delete the `postgres`, `nats`, `seaweedfs` restart-policy entries (those services are dev-only now); the `loki`, `promtail`, `auto-verdict-grafana` entries stay.

## Secrets — user edits at the flip point

- vps-gateway `SEAWEEDFS_S3_CONFIG`: full JSON provided (fartoff identity + new autoverdict identity).
- auto-verdict `VPS_ENV_PRD`:
  - `DATABASE_URL=Host=gateway-postgres;Port=5432;Database=autoverdict;Username=autoverdict;Password=<new>`
  - `NATS_URL=nats://gateway-nats:4222`
  - `S3_ENDPOINT=http://gateway-seaweedfs:8333`, `S3_ACCESS_KEY=<new>`, `S3_SECRET_KEY=<new>` (`S3_BUCKET=auto-verdict-local` unchanged)
  - `GRAFANA_DB_PASSWORD=<same as autoverdict db password>`

## Execution runbook (summary — full commands in the implementation plan)

**Phase A — prep, zero auto-verdict downtime:**
1. vps-gateway repo changes; user updates `SEAWEEDFS_S3_CONFIG`; deploy gateway (fartoff DB blip ~10 s; seaweedfs recreate seconds).
2. Create `autoverdict` role + DB on gateway postgres (psql; generated password).
3. Create bucket `auto-verdict-local` via `weed shell`; smoke-test new S3 creds (put/get/delete round-trip).
4. Initial `mc mirror` old → new (21 GB; one-off container attached to both `auto-verdict_default` and `caddy-gateway`).
5. Create stream `AUTOVERDICT_CHECKS` on `gateway-nats` (one-off nats-box with the repo's stream JSON).
6. Verify backup sidecar produced its first dump.

**Phase B — cutover, ~5–10 min API downtime:**
7. Stop auto-verdict api + processing (frontend keeps serving; API calls fail during the window).
8. `pg_dump` autoverdict → `~/backups/` on VPS (migration dump = backup #1) → restore into gateway DB.
9. Final `mc mirror` delta (quiesced writes).
10. User updates `VPS_ENV_PRD`; merge auto-verdict changes; run Deploy (PRD) — `--remove-orphans` removes the local infra containers (volumes retained).
11. Verify: `/api/me` 401; user does a real login; an existing report opens (S3 read); NATS consumer ready in processing logs; Grafana dashboards render; fartoff unaffected.

**Phase C — cleanup after 7-day soak (≈2026-07-13):** remove old `postgres_data`/`nats_data`/`seaweedfs_data` volumes.

## Rollback

- Any point in Phase A/B before step 10: nothing user-facing changed; abort freely.
- After step 10: revert `VPS_ENV_PRD` + `git revert` the compose commit + redeploy. Old volumes untouched until Phase C. Data written to gateway DB between flip and rollback would be lost (minutes-old checks at most — accepted).

## Error handling / verification gates

Each runbook step has an explicit check (row counts after restore, `mc diff` empty after final mirror, stream info present, S3 round-trip). Failure at any gate stops the migration before the flip; the flip itself is the only step that changes user-facing behavior.
