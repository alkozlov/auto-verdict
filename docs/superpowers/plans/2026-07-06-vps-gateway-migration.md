# VPS-Gateway Infra Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. NOTE: this plan is a production-ops runbook — Tasks 3–8 run against the live VPS over SSH and interact with the human for secret updates; they must be executed by the controller session itself, in order, never in parallel. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Move auto-verdict's postgres (with prod data), SeaweedFS (21 GB, with credential rotation), and NATS onto the shared vps-gateway stack, add nightly postgres backups, and retire auto-verdict's own infra containers.

**Architecture:** Copy-and-flip. Phase A prepares the gateway (aliases, gateway-nats, backup sidecar, autoverdict DB/bucket/creds) and mirrors data with zero downtime; Phase B is a ~5–10 min cutover (stop API → final dump/restore + delta mirror → flip `VPS_ENV_PRD` → redeploy). Old volumes stay untouched for a 7-day soak.

**Tech Stack:** docker compose on VPS (SSH alias `vps`, remote shell zsh — quote everything), postgres 17.10 `pg_dump`/`psql`, `minio/mc` for S3 mirroring, `natsio/nats-box` for stream creation, GitHub Actions `workflow_dispatch` deploys.

**Spec:** `docs/superpowers/specs/2026-07-06-vps-gateway-migration-design.md`

## Global Constraints

- New cross-project DNS aliases on `caddy-gateway`: `gateway-postgres`, `gateway-seaweedfs`, `gateway-nats`. Legacy `postgres`/`seaweedfs` aliases KEPT (fartoff depends on them).
- SeaweedFS image pinned to `chrislusf/seaweedfs:4.37` (what `:latest` already runs).
- gateway-nats: `nats:2.10-alpine`, JetStream `store_dir /data`, `max_memory_store 1GB`, `max_file_store 10GB`, `http_port 8222`.
- Backups: nightly `pg_dumpall` (all DBs), gzip, volume `postgres-backups`, 7-day retention.
- autoverdict S3 identity: bucket-scoped actions only (`Read:auto-verdict-local` etc.), NO Admin. Bucket name stays `auto-verdict-local`.
- Secrets NEVER go into git: real values are generated at execution time and handed to the human; this plan uses `<AV_PG_PASSWORD>`, `<AV_S3_ACCESS_KEY>`, `<AV_S3_SECRET_KEY>` placeholders.
- auto-verdict compose changes are committed locally in Task 2 but pushed only at cutover (Task 7), so main never describes infra that prod isn't running yet.
- Every data step has a verification gate; stop the migration at any failed gate before Task 7's env flip — until then nothing user-facing has changed.
- Old volumes (`auto-verdict_postgres_data`, `auto-verdict_nats_data`, `auto-verdict_seaweedfs_data`) are deleted only in Task 9, after the 7-day soak (~2026-07-13).

---

### Task 1: vps-gateway repo changes

**Files:**
- Modify: `C:\Users\AKazlou\Projects\vps-gateway\docker-compose.yml`
- Create: `C:\Users\AKazlou\Projects\vps-gateway\nats\nats.conf`
- Modify: `C:\Users\AKazlou\Projects\vps-gateway\.github\workflows\deploy.yml` (scp `source:` line)

**Interfaces:**
- Produces: DNS names `gateway-postgres`, `gateway-seaweedfs`, `gateway-nats` on `caddy-gateway`; volume `postgres-backups` with nightly dumps; volume `gateway-nats-data`.

- [ ] **Step 1: compose — aliases, pin, gateway-nats, postgres-backup**

In `docker-compose.yml`, change the `postgres` service networks block to:

```yaml
    networks:
      caddy-gateway:
        aliases:
          - postgres          # legacy — fartoff connects via this name
          - gateway-postgres  # cross-project convention
```

Change the `seaweedfs` service image line and networks block to:

```yaml
    image: chrislusf/seaweedfs:4.37
```
```yaml
    networks:
      caddy-gateway:
        aliases:
          - seaweedfs           # legacy
          - gateway-seaweedfs   # cross-project convention
```

Add two services (after `seaweedfs`, before `caddy`):

```yaml
  gateway-nats:
    image: nats:2.10-alpine
    command: ['-c', '/etc/nats/nats.conf']
    volumes:
      - gateway-nats-data:/data
      - ./nats/nats.conf:/etc/nats/nats.conf:ro
    healthcheck:
      test: ['CMD-SHELL', 'wget -qO- http://localhost:8222/healthz']
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 5s
    networks:
      - caddy-gateway
    restart: unless-stopped

  postgres-backup:
    image: postgres:17
    entrypoint:
      - sh
      - -c
      - |
        while true; do
          ts=$$(date +%Y%m%d-%H%M%S)
          pg_dumpall | gzip > "/backups/pg-$$ts.sql.gz" \
            && echo "backup pg-$$ts.sql.gz written" \
            || echo "backup FAILED at $$ts"
          find /backups -name 'pg-*.sql.gz' -mtime +7 -delete
          sleep 86400
        done
    environment:
      PGHOST: postgres
      PGUSER: ${POSTGRES_FARTOFF_USER}
      PGPASSWORD: ${POSTGRES_FARTOFF_PASSWORD}
    volumes:
      - postgres-backups:/backups
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - caddy-gateway
    restart: unless-stopped
```

Add to the top-level `volumes:` block:

```yaml
  gateway-nats-data:
  postgres-backups:
```

- [ ] **Step 2: nats config**

Create `nats/nats.conf` (identical semantics to auto-verdict's current config):

```
jetstream {
  store_dir: /data
  max_memory_store: 1GB
  max_file_store: 10GB
}

http_port: 8222
```

- [ ] **Step 3: ship the nats dir on deploy**

In `.github/workflows/deploy.yml`, change the scp step's source line to:

```yaml
          source: "Caddyfile,docker-compose.yml,nats"
```

- [ ] **Step 4: validate and commit**

Run from `C:\Users\AKazlou\Projects\vps-gateway`:
`docker compose config --quiet` → exit 0, no errors (warnings about unset `POSTGRES_FARTOFF_*` acceptable locally).

```bash
git add docker-compose.yml nats/nats.conf .github/workflows/deploy.yml
git commit -m "feat: gateway aliases, gateway-nats, nightly postgres backups, pin seaweedfs 4.37"
git push origin main
```

---

### Task 2: auto-verdict repo changes (commit local, push at cutover)

**Files:**
- Modify: `docker-compose.yml` (remove 5 infra services + their volumes + api `depends_on`; rename `processing-service` → `auto-verdict-processing` with `caddy-gateway`)
- Modify: `docker-compose.override.yml` (add the 5 infra services, volumes, dev `depends_on`)
- Modify: `docker-compose.prod.yml` (rename processing key; drop postgres/nats/seaweedfs entries)
- Modify: `infra/grafana/provisioning/datasources/postgres.yml` (gateway host + env-injected password)
- Delete: `.github/workflows/deploy-infra.yml`

**Interfaces:**
- Consumes: `gateway-postgres`/`gateway-seaweedfs`/`gateway-nats` DNS names (Task 1).
- Produces: prod compose that runs only app + observability services; dev compose identical to today's behavior.

- [ ] **Step 1: base compose**

In `docker-compose.yml`:
- Delete the entire `postgres`, `nats`, `nats-setup`, `seaweedfs`, `seaweedfs-setup` service blocks.
- Delete `postgres_data`, `nats_data`, `seaweedfs_data` from the `volumes:` block (keep `grafana_data`, `loki_data`).
- In `auto-verdict-api`: delete the whole `depends_on:` block (dev-only ordering moves to the override).
- Rename service `processing-service:` → `auto-verdict-processing:` and add at the end of its block:

```yaml
    networks:
      - default
      - caddy-gateway
```

- In `auto-verdict-grafana` environment, add:

```yaml
      GRAFANA_DB_URL: ${GRAFANA_DB_URL:-gateway-postgres:5432}
      GRAFANA_DB_PASSWORD: ${GRAFANA_DB_PASSWORD:-autoverdict}
```

- [ ] **Step 2: override compose (dev parity)**

Append to `docker-compose.override.yml` — the five service blocks exactly as deleted from base (including their images, commands, volumes, healthchecks — copy verbatim from the pre-edit base compose), plus dev ports already present in the override today, plus:

```yaml
  auto-verdict-api:
    depends_on:
      postgres:
        condition: service_healthy
      nats-setup:
        condition: service_completed_successfully
      seaweedfs-setup:
        condition: service_completed_successfully

  auto-verdict-processing:
    depends_on:
      postgres:
        condition: service_healthy
      nats-setup:
        condition: service_completed_successfully
      seaweedfs-setup:
        condition: service_completed_successfully

  auto-verdict-grafana:
    environment:
      GRAFANA_DB_URL: postgres:5432

volumes:
  postgres_data:
  nats_data:
  seaweedfs_data:
```

(Merge with the existing override keys: the existing `api`→`auto-verdict-api` port mapping and `processing-service`→`auto-verdict-processing` env blocks are renamed in place; postgres/nats/seaweedfs port mappings fold into their moved service blocks.)

- [ ] **Step 3: prod overlay**

In `docker-compose.prod.yml`: rename `processing-service:` key → `auto-verdict-processing:`; delete the `postgres:`, `nats:`, `seaweedfs:` entries. `loki`, `promtail`, `auto-verdict-grafana` entries stay.

- [ ] **Step 4: grafana datasource**

Replace `infra/grafana/provisioning/datasources/postgres.yml` content:

```yaml
apiVersion: 1

datasources:
  - name: PostgreSQL
    uid: postgresql
    type: grafana-postgresql-datasource
    access: proxy
    url: $__env{GRAFANA_DB_URL}
    database: autoverdict
    user: autoverdict
    secureJsonData:
      password: $__env{GRAFANA_DB_PASSWORD}
    jsonData:
      sslmode: disable
      maxOpenConns: 10
      maxIdleConns: 2
      connMaxLifetime: 14400
      postgresVersion: 1700
      timescaledb: false
    editable: true
```

- [ ] **Step 5: delete deploy-infra.yml, validate both combos, commit (NO push)**

```bash
git rm .github/workflows/deploy-infra.yml
docker compose config --quiet                                             # dev combo (base+override)
docker compose -f docker-compose.yml -f docker-compose.prod.yml config --quiet  # prod combo
```
Both exit 0. Then confirm the prod combo contains NO postgres/nats/seaweedfs services and that `auto-verdict-processing` is on `caddy-gateway`:
`docker compose -f docker-compose.yml -f docker-compose.prod.yml config | grep -E "^  [a-z-]+:$"` → only app + loki/promtail/grafana services.

```bash
git add -A
git commit -m "feat: consume shared gateway infra; local infra becomes dev-only"
# DO NOT push — push happens in Task 7 at cutover.
```

---

### Task 3: generate credentials, update SEAWEEDFS_S3_CONFIG, deploy gateway

- [ ] **Step 1: generate secrets (controller, never committed)**

```bash
python -c "import secrets; print('AV_PG_PASSWORD='+secrets.token_urlsafe(24)); print('AV_S3_ACCESS_KEY='+secrets.token_hex(10)); print('AV_S3_SECRET_KEY='+secrets.token_urlsafe(32))"
```
Save to the session scratchpad file `migration-secrets.txt` (NOT in either repo).

- [ ] **Step 2: human updates vps-gateway secret `SEAWEEDFS_S3_CONFIG`**

Hand the human this identity block to append to the existing `identities` array (keeping the fartoff identity exactly as-is):

```json
    {
      "name": "autoverdict",
      "credentials": [
        { "accessKey": "<AV_S3_ACCESS_KEY>", "secretKey": "<AV_S3_SECRET_KEY>" }
      ],
      "actions": [
        "Read:auto-verdict-local",
        "Write:auto-verdict-local",
        "List:auto-verdict-local",
        "Tagging:auto-verdict-local"
      ]
    }
```

WAIT for confirmation before deploying.

- [ ] **Step 3: deploy gateway**

```bash
cd C:\Users\AKazlou\Projects\vps-gateway && gh workflow run deploy.yml && gh run watch <run-id> --exit-status
```
Expected: postgres + seaweedfs recreated (≈10 s fartoff DB blip), gateway-nats + postgres-backup created.

- [ ] **Step 4: verification gate**

```bash
ssh vps 'docker ps --format "{{.Names}} {{.Status}}" | grep -E "gateway"'          # nats + backup + postgres + seaweedfs all Up
ssh vps 'docker exec vps-gateway-gateway-nats-1 wget -qO- http://localhost:8222/healthz'   # {"status":"ok"}
curl -s -o /dev/null -w "%{http_code}\n" https://fartoff.com/ --max-time 15         # 200 — fartoff survived the blips
ssh vps 'docker run --rm --network caddy-gateway alpine sh -c "getent hosts gateway-postgres gateway-seaweedfs gateway-nats"'  # all three resolve
```
After ≤1 min, backup sidecar wrote its first dump:
```bash
ssh vps 'docker exec vps-gateway-postgres-backup-1 ls -la /backups'                 # pg-*.sql.gz present, size > 1KB
```

---

### Task 4: provision autoverdict DB, bucket, creds test

- [ ] **Step 1: role + database** (use real password from `migration-secrets.txt`)

```bash
ssh vps 'source ~/vps-gateway/.env 2>/dev/null; docker exec vps-gateway-postgres-1 psql -U "$POSTGRES_FARTOFF_USER" -d postgres -c "CREATE ROLE autoverdict LOGIN PASSWORD '"'"'<AV_PG_PASSWORD>'"'"';" -c "CREATE DATABASE autoverdict OWNER autoverdict;"'
```
Gate: `\l` shows `autoverdict | autoverdict` owner; `psql -U autoverdict -d autoverdict -c "select 1"` (with PGPASSWORD) returns 1.

- [ ] **Step 2: bucket via weed shell (no S3 creds needed)**

```bash
ssh vps 'docker exec vps-gateway-seaweedfs-1 sh -c "echo \"s3.bucket.create -name auto-verdict-local\" | weed shell -master localhost:9333"'
```
Gate: `s3.bucket.list` shows `auto-verdict-local`.

- [ ] **Step 3: S3 round-trip with the NEW autoverdict creds**

```bash
ssh vps 'docker run --rm --network caddy-gateway --entrypoint sh minio/mc:RELEASE.2024-11-17T19-35-25Z -c "mc alias set gw http://gateway-seaweedfs:8333 <AV_S3_ACCESS_KEY> <AV_S3_SECRET_KEY> --api S3v4 && echo migration-test | mc pipe gw/auto-verdict-local/migration-test.txt && mc cat gw/auto-verdict-local/migration-test.txt && mc rm gw/auto-verdict-local/migration-test.txt"'
```
Gate: prints `migration-test`, removes cleanly. (Also proves the bucket-scoped identity works without Admin.)

---

### Task 5: initial 21 GB mirror (zero downtime)

- [ ] **Step 1: start a dual-network mc container**

```bash
ssh vps 'docker run -d --name av-mirror --network caddy-gateway --entrypoint sleep minio/mc:RELEASE.2024-11-17T19-35-25Z infinity && docker network connect auto-verdict_default av-mirror'
```
IMPORTANT: the OLD instance must be addressed by container name `auto-verdict-seaweedfs-1` — the bare name `seaweedfs` is ambiguous for this dual-homed container (gateway's alias vs old service).

- [ ] **Step 2: aliases + detached mirror**

```bash
ssh vps 'docker exec av-mirror mc alias set old http://auto-verdict-seaweedfs-1:8333 dev-access-key dev-secret-key --api S3v4 && docker exec av-mirror mc alias set new http://gateway-seaweedfs:8333 <AV_S3_ACCESS_KEY> <AV_S3_SECRET_KEY> --api S3v4 && docker exec -d av-mirror sh -c "mc mirror --preserve old/auto-verdict-local new/auto-verdict-local > /tmp/mirror.log 2>&1"'
```

- [ ] **Step 3: poll until done, then gate**

Poll every few minutes (21 GB local copy ≈ 5–15 min):
```bash
ssh vps 'docker exec av-mirror sh -c "tail -3 /tmp/mirror.log; pgrep -f \"mc mirror\" >/dev/null && echo RUNNING || echo DONE"'
```
Gate when DONE: `docker exec av-mirror mc diff old/auto-verdict-local new/auto-verdict-local` → empty output (allow new writes since mirror start; those re-sync in Task 7).

---

### Task 6: NATS stream on gateway-nats

- [ ] **Step 1: create stream from the repo's config (already on VPS from prior deploys)**

```bash
ssh vps 'docker run --rm --network caddy-gateway -v ~/auto-verdict/infra/nats/streams:/streams:ro natsio/nats-box:0.14.5 nats --server nats://gateway-nats:4222 stream add --config /streams/car-checks.json'
```

- [ ] **Step 2: gate**

```bash
ssh vps 'docker run --rm --network caddy-gateway natsio/nats-box:0.14.5 nats --server nats://gateway-nats:4222 stream info AUTOVERDICT_CHECKS'
```
Expected: stream exists, subjects `autoverdict.checks.>`, file storage, 24h max age.

---

### Task 7: CUTOVER (~5–10 min API downtime)

- [ ] **Step 1: announce start; stop API + processing**

```bash
ssh vps 'docker stop auto-verdict-auto-verdict-api-1 auto-verdict-processing-service-1'
```
(Frontend keeps serving; API requests fail until Step 6 completes.)

- [ ] **Step 2: dump (backup #1) and restore**

```bash
ssh vps 'mkdir -p ~/backups && docker exec auto-verdict-postgres-1 pg_dump -U autoverdict -d autoverdict --no-owner --no-privileges | gzip > ~/backups/autoverdict-pre-gateway-migration.sql.gz && ls -la ~/backups'
ssh vps 'gunzip -c ~/backups/autoverdict-pre-gateway-migration.sql.gz | docker exec -i -e PGPASSWORD='"'"'<AV_PG_PASSWORD>'"'"' vps-gateway-postgres-1 psql -U autoverdict -d autoverdict -v ON_ERROR_STOP=1 -q'
```

- [ ] **Step 3: row-count gate (must match exactly)**

```bash
ssh vps 'for t in users car_checks refresh_tokens credit_ledger payment_orders outbox_messages uploaded_files ai_runs; do old=$(docker exec auto-verdict-postgres-1 psql -U autoverdict -d autoverdict -tAc "select count(*) from $t" 2>/dev/null); new=$(docker exec -e PGPASSWORD='"'"'<AV_PG_PASSWORD>'"'"' vps-gateway-postgres-1 psql -U autoverdict -d autoverdict -tAc "select count(*) from $t" 2>/dev/null); echo "$t: old=$old new=$new"; done'
```
(Table names: verify against `\dt` first — use actual snake_case names.) Every row must show old=new.

- [ ] **Step 4: final mirror delta + gate**

```bash
ssh vps 'docker exec av-mirror mc mirror --preserve old/auto-verdict-local new/auto-verdict-local && docker exec av-mirror mc diff old/auto-verdict-local new/auto-verdict-local'
```
Gate: `mc diff` output EMPTY (writers are stopped now).

- [ ] **Step 5: human updates `VPS_ENV_PRD`** (exact lines, real values from `migration-secrets.txt`):

```
DATABASE_URL=Host=gateway-postgres;Port=5432;Database=autoverdict;Username=autoverdict;Password=<AV_PG_PASSWORD>
NATS_URL=nats://gateway-nats:4222
S3_ENDPOINT=http://gateway-seaweedfs:8333
S3_ACCESS_KEY=<AV_S3_ACCESS_KEY>
S3_SECRET_KEY=<AV_S3_SECRET_KEY>
GRAFANA_DB_PASSWORD=<AV_PG_PASSWORD>
```
(`S3_BUCKET=auto-verdict-local` unchanged.) WAIT for confirmation.

- [ ] **Step 6: push + deploy**

```bash
cd C:\Users\AKazlou\Projects\auto-verdict && git push origin main
gh workflow run deploy.yml -f environment=PRD && gh run watch <id> --exit-status
```
`--remove-orphans` removes the old postgres/nats/seaweedfs containers (volumes retained).

- [ ] **Step 7: verification battery**

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://autoverdict.app/api/me                      # 401
curl -s -o /dev/null -w "%{http_code}\n" -X POST https://autoverdict.app/api/auth/refresh   # 401
curl -s -o /dev/null -w "%{http_code}\n" https://autoverdict.app/ --max-time 15             # 200
curl -s -o /dev/null -w "%{http_code}\n" https://fartoff.com/ --max-time 15                 # 200
curl -s -o /dev/null -w "%{http_code}\n" https://grafana.autoverdict.app/ --max-time 15     # 302
ssh vps 'docker logs $(docker ps -q --filter name=auto-verdict-api) 2>&1 | grep -iE "No migrations|Applying|listening|error" | head'   # migrations up to date vs gateway DB, listening
ssh vps 'docker logs $(docker ps -q --filter name=auto-verdict-processing) 2>&1 | grep -iE "NATS|consumer|error" | head'              # consumer ready on gateway-nats
ssh vps 'docker ps --format "{{.Names}}" | grep -E "auto-verdict-(postgres|nats|seaweedfs)" || echo "old infra containers gone"'
```
Human verifies: real Google login works; an EXISTING report opens (proves S3 reads from gateway); Grafana dashboards render (proves datasource).

**Rollback (only if a gate here fails):** human reverts `VPS_ENV_PRD` to previous values → `git revert` the Task 2 commit, push → run Deploy PRD → old volumes/containers come back exactly as before (their volumes were never touched).

---

### Task 8: post-cutover cleanup (same session)

- [ ] **Step 1: remove the mirror helper**

```bash
ssh vps 'docker rm -f av-mirror'
```

- [ ] **Step 2: copy migration dump off-box** (first off-VPS backup)

```bash
scp vps:~/backups/autoverdict-pre-gateway-migration.sql.gz "C:\Users\AKazlou\Projects\auto-verdict-backups\"
```
(Create the local dir first; it lives OUTSIDE the repo.)

- [ ] **Step 3: record the soak deadline**

Memory note: old volumes `auto-verdict_postgres_data`, `auto-verdict_nats_data`, `auto-verdict_seaweedfs_data` (21 GB) deletable after 2026-07-13 if no regressions. Update infra-consolidation memory: migration complete.

---

### Task 9: volume deletion (deferred to ≈2026-07-13, NOT this session)

```bash
ssh vps 'docker volume rm auto-verdict_postgres_data auto-verdict_nats_data auto-verdict_seaweedfs_data'
```
Only after the human confirms a week of normal operation.
