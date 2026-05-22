# Product Requirements Document

## 1. Product Name

Working name: **Auto Verdict**.

Domain candidate: `auto-verdict.app`.

The name is not final. During MVP, validating the product hypothesis is more important than final branding.

## 2. Product Vision

Build a SaaS product that helps inexperienced used car buyers make safer preliminary decisions about car listings by detecting potential risks, inconsistencies, missing information, suspicious wording, and practical follow-up questions.

The product acts as an AI-assisted screening tool. It does not replace:

- professional vehicle inspection;
- mechanic diagnostics;
- official vehicle history reports;
- legal verification;
- independent expert evaluation.

## 3. Problem Statement

Buying a used car involves significant financial risk. Inexperienced buyers often struggle to:

- notice suspicious wording in listings;
- compare seller claims with vehicle history;
- understand common problems of a specific model;
- know which documents to request;
- decide whether a listing is worth further attention;
- prepare for a seller call or vehicle inspection.

Many listings look attractive but may hide problematic history: company ownership, import, accidents, incomplete service records, mileage inconsistencies, short ownership periods, vague claims, or mismatches between listing text and public records.

## 4. Target Users

### Primary Persona: Inexperienced Private Buyer

A private buyer who:

- is looking for a family car;
- is not a car expert;
- has limited experience with the used car market;
- may have previously bought only new cars from dealerships;
- fears dishonest sellers or hidden defects;
- is willing to pay a few euros for a preliminary listing check;
- compares multiple listings and wants to filter risky ones quickly.

### Secondary Persona: Cautious Buyer

A user who has some experience but wants a structured second opinion before contacting sellers.

### Future Persona: Car Selection Assistant / Small Broker

A user who reviews many listings for clients and needs quick, structured pre-screening.

This persona is outside MVP scope.

## 5. Initial Market

- Country: Poland.
- Primary marketplace: Otomoto.pl.
- Vehicle segment: relatively recent used cars, approximately model year 2022 or newer.
- Language: initially English for internal documentation and system prompts; product UI may later support Polish, English, and Russian.

## 6. Value Proposition

The system gives users a practical, structured AI report that answers:

- Does this listing look risky?
- What information is missing?
- Are there inconsistencies?
- What questions should I ask the seller?
- What should I verify before inspection?
- What model-specific issues should I know about?
- Should I proceed, request more information, or avoid the listing?

## 7. MVP Scope

### Included in MVP

- Backend-owned Google authentication.
- Backend-issued auth cookie or JWT used by the frontend.
- New user receives 1–2 free checks.
- User can buy:
  - one check;
  - a package of five checks.
- User can create a car check.
- User can provide:
  - listing URL;
  - listing text;
  - screenshots;
  - optional VIN;
  - optional registration number;
  - optional first registration date;
  - optional pasted official/public vehicle history information.
- System stores uploaded screenshots in object storage.
- System stores check data in PostgreSQL.
- API submits analysis task to NATS JetStream.
- ProcessingService consumes task and calls Claude API.
- System generates structured AI report.
- User can view previous checks and reports.
- System handles failed processing states.
- Minimal protected internal/admin operations for failed checks, retries, manual refunds, credit balance inspection, and processing status inspection.

### Excluded from MVP

- Full automatic Otomoto scraping.
- Guaranteed official vehicle history integration.
- Browser extension.
- Mobile app.
- Subscription billing.
- B2B workflows.
- Multi-marketplace Europe-wide support.
- Automatic VIN lookup on paid third-party services.
- PDF generation.
- Complex admin panel.
- Presigned direct browser uploads to object storage.
- NextAuth/Auth.js as the authoritative authentication system.

## 8. User Journey

### First-time User

1. User lands on the website.
2. User signs in with Google.
3. System creates a user account.
4. System grants initial free check credits.
5. User creates the first car check.
6. User enters listing data and uploads screenshots.
7. System validates input.
8. System queues analysis.
9. User sees processing status.
10. User opens the completed report.

### Returning User

1. User signs in.
2. User sees remaining credits and previous checks.
3. User creates another check.
4. If credits are available, the check starts.
5. If credits are not available, the user buys one check or a package of five.

## 9. Pricing Model

MVP supports credits.

- New users receive a configurable number of free credits.
- One credit equals one car check.
- Payment options:
  - single check;
  - package of five checks.

The backend must treat Stripe webhooks as authoritative for successful payments.

Payments should be implemented after the free-check lifecycle works end to end:

- user registration;
- free credits;
- check creation;
- file upload;
- outbox publishing;
- ProcessingService analysis;
- Claude report generation;
- report display.

## 10. Core Report Structure

The AI report must contain:

- overall risk level: low, medium, high, or unknown;
- confidence level;
- summary;
- extracted vehicle facts;
- positive signals;
- risk signals;
- missing information;
- seller questions;
- inspection checklist;
- model-specific risks;
- final recommendation;
- disclaimer.

MVP reports must be generated in English. Later versions may add selectable report languages.

## 11. Positioning and Safety

The product must avoid presenting its output as a final guarantee.

Avoid absolute statements such as:

- “This car is safe.”
- “This seller is dishonest.”
- “This car was definitely damaged.”

Prefer cautious wording:

- “This may indicate a risk.”
- “This should be clarified with the seller.”
- “The available data is insufficient.”
- “A professional inspection is recommended.”

## 12. Success Metrics

MVP success can be measured by:

- number of registered users;
- number of created checks;
- percentage of users who use free checks;
- percentage of users who buy paid credits;
- average checks per user;
- report completion rate;
- AI processing failure rate;
- user feedback on report usefulness.

## 13. Key Risks

- AI hallucination or unsupported claims.
- Low-quality user input.
- Marketplace scraping limitations.
- Legal risk from overly definitive recommendations.
- Payment/webhook errors.
- Storage and data cleanup issues.
- Message processing failures.
- Claude API cost and availability.

## 14. MVP Hypothesis

A private buyer considering a used car worth thousands of euros is willing to pay a small fee for a clear AI-assisted preliminary analysis that helps avoid risky listings and prepares them for seller communication.
