# Data Model

This document describes the conceptual PostgreSQL data model for the MVP.

The exact schema may evolve during implementation.

## 1. users

Stores application users.

```txt
id uuid primary key
email text not null unique
display_name text null
avatar_url text null
created_at timestamptz not null
updated_at timestamptz not null
last_login_at timestamptz null
```

## 2. external_auth_accounts

Stores OAuth identities.

```txt
id uuid primary key
user_id uuid not null references users(id)
provider text not null
provider_user_id text not null
email text not null
created_at timestamptz not null

unique(provider, provider_user_id)
```

## 3. user_credits

Stores current check balance.

```txt
user_id uuid primary key references users(id)
available_checks int not null
updated_at timestamptz not null
```

Rules:

- balance must not become negative;
- every credit change should have a corresponding ledger entry.

## 4. credit_ledger

Stores credit balance changes.

```txt
id uuid primary key
user_id uuid not null references users(id)
change_amount int not null
reason text not null
related_check_id uuid null
related_payment_id uuid null
created_at timestamptz not null
```

Example reasons:

```txt
initial_free_credits
purchase_single_check
purchase_pack_5
check_started
technical_refund
manual_adjustment
```

## 5. payments

Stores payment records.

```txt
id uuid primary key
user_id uuid not null references users(id)
stripe_checkout_session_id text null
stripe_payment_intent_id text null
product_code text not null
credits_granted int not null
amount_total int null
currency text null
status text not null
created_at timestamptz not null
updated_at timestamptz not null
```

Product codes:

```txt
single_check
pack_5_checks
```

## 6. stripe_events

Stores processed Stripe webhook events for idempotency.

```txt
id uuid primary key
stripe_event_id text not null unique
event_type text not null
payload_json jsonb not null
processed_at timestamptz not null
processing_status text not null
error_message text null
```

## 7. car_checks

Stores car check records.

```txt
id uuid primary key
user_id uuid not null references users(id)
status text not null
listing_url text null
listing_title text null
listing_text text null
user_notes text null
vin text null
registration_number text null
first_registration_date date null
price_amount numeric null
price_currency text null
created_at timestamptz not null
updated_at timestamptz not null
queued_at timestamptz null
processing_started_at timestamptz null
completed_at timestamptz null
failed_at timestamptz null
failure_reason text null
```

Statuses:

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

## 8. car_check_inputs

Stores normalized structured input for a check.

```txt
id uuid primary key
check_id uuid not null references car_checks(id)
input_json jsonb not null
created_at timestamptz not null
```

This table can store normalized data extracted from listing text and user input.

## 9. uploaded_files

Stores metadata for uploaded files.

```txt
id uuid primary key
user_id uuid not null references users(id)
check_id uuid not null references car_checks(id)
storage_provider text not null
bucket text not null
object_key text not null
original_file_name text not null
content_type text not null
size_bytes bigint not null
checksum text null
status text not null
created_at timestamptz not null
deleted_at timestamptz null
```

Storage provider for MVP:

```txt
seaweedfs
```

## 10. car_reports

Stores completed AI reports.

```txt
id uuid primary key
check_id uuid not null unique references car_checks(id)
user_id uuid not null references users(id)
risk_level text not null
confidence text not null
summary text not null
report_json jsonb not null
created_at timestamptz not null
```

## 11. ai_requests

Stores AI request metadata.

```txt
id uuid primary key
check_id uuid not null references car_checks(id)
provider text not null
model text not null
prompt_version text not null
status text not null
started_at timestamptz not null
completed_at timestamptz null
input_tokens int null
output_tokens int null
total_tokens int null
error_message text null
```

The prompt body may be stored separately or omitted depending on privacy and cost-analysis requirements.

MVP must not store full prompt bodies, raw AI input, or raw AI output by default.

If needed for debugging, raw prompt/input/output storage may be added behind an explicit development-only configuration flag. It must be disabled by default and must not be enabled in production unless explicitly configured.

## 12. outbox_messages

Stores messages that must be published to NATS JetStream.

```txt
id uuid primary key
type text not null
subject text not null
payload_json jsonb not null
status text not null
created_at timestamptz not null
published_at timestamptz null
retry_count int not null default 0
last_error text null
```

Statuses:

```txt
Pending
Published
Failed
```

## 13. inbox_messages

Stores consumed message ids to support idempotency.

```txt
id uuid primary key
message_id uuid not null
consumer_name text not null
processed_at timestamptz not null

unique(message_id, consumer_name)
```

## 14. processing_errors

Stores processing failures for diagnostics.

```txt
id uuid primary key
check_id uuid not null references car_checks(id)
message_id uuid null
error_type text not null
error_message text not null
error_details text null
created_at timestamptz not null
```

## 15. Suggested Indexes

```txt
users(email)
car_checks(user_id, created_at desc)
car_checks(status)
car_reports(check_id)
uploaded_files(check_id)
payments(user_id, created_at desc)
stripe_events(stripe_event_id)
outbox_messages(status, created_at)
inbox_messages(message_id, consumer_name)
ai_requests(check_id, started_at desc)
```

## 16. Ownership Rules

API service primarily owns:

- users;
- external_auth_accounts;
- user_credits;
- credit_ledger;
- payments;
- stripe_events;
- initial car_checks creation;
- uploaded_files metadata;
- outbox_messages creation.

ProcessingService primarily owns:

- car_checks processing transitions;
- car_reports;
- ai_requests;
- inbox_messages;
- processing_errors.
