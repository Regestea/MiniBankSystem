# MiniBankSystem

![CI](https://github.com/Regestea/MiniBankSystem/actions/workflows/ci.yml/badge.svg)
`net10.0` · Clean Architecture + DDD · CQRS · Double-entry ledger · EF Core (PostgreSQL) + Dapper reads

Sample (portfolio-only) mini banking backend. **Not for commercial/production use** — no real money, USD-only demo logic, relaxed operational concerns. Frontend not included yet (`Backend.http` + Scalar cover API exploration).

## What it does

- Customer registration (two-phase: `IdentityUser` via UserManager commits immediately, then `Customer + CustomerRisk` share one `Guid` in one `SaveChanges`; phase-2 failure compensates by deleting the orphan IdentityUser)
- Accounts: open → approve/reject → freeze/unfreeze → close (zero-balance), deposit / withdraw / transfer
- Double-entry ledger (`ledger_entries` owned by `Account` is the balance source of truth)
- Daily risk limits per level (Low 10k/10, Medium 5k/5, High 1k/3, UTC-day windows) — outflows only; deposits are intentionally outside limits
- KYC + documents + audit logs + admin endpoints (admins bypass ownership; admin-only commands re-check `IsAdmin` in the handler, not just `[Authorize(Roles="Admin")]`)
- Auth: ASP.NET Core Identity API (`/login`, `/refresh`, …) with `/register` disabled — registration goes through `POST /customers`. Bearer-token API: no CSRF/antiforgery by design.
- Idempotent money operations via `IdempotencyKey` (client-generated, max 64 chars, GLOBAL `ux_transactions_reference`)

## Architecture

```
Backend/
  MiniBank.Api            → Controllers, Auth, RateLimiting, CORS, Scalar/OpenAPI (no antiforgery: bearer-token API)
  MiniBank.Features       → CQRS (Command/Query + Handler), hand-rolled Mediator, FluentValidation, shared IdempotencyKeys
  MiniBank.Domain         → Aggregates (Account, Transaction, Customer, Risk, Kyc, Document, Audit),
                            ValueObjects (Money, AccountNumber, Email, …), DomainEvents
  MiniBank.Infrastructure → EF Core (Npgsql) writes, Dapper reads, Identity, UoW, Middleware, Migrations
  MiniBank.Abstractions   → ICurrentUserContext, IAccessGuard, IIdentityUserService, ISqlConnectionFactory
Backend.Tests/ (5 projects: Domain, Features, Infrastructure, Api, Architecture)
MiniBank.AppHost + MiniBank.ServiceDefaults (Aspire)
```

Request flow: `Controller → Mediator (validate) → Handler → Domain (Account/Transaction/Risk) → EfUnitOfWork.SaveChanges (audit inline, events out-of-band)` · Reads that need speed (statement, accounts list) use Dapper `SUM` directly.

## Tech stack

.NET 10, EF Core + Npgsql (PostgreSQL 16/18), Dapper, ASP.NET Core Identity (Guid keys), FluentValidation, Scalar + OpenAPI, Aspire (AppHost with Postgres + PgAdmin), xUnit + FluentAssertions + NSubstitute + NetArchTest, Testcontainers (infra tests), EF InMemory (API tests).

## Getting started

### Option A — Aspire (easiest)

```bash
dotnet run --project MiniBank.AppHost
```

Starts Postgres (persisted volume) + PgAdmin, wires `ConnectionStrings:minibankdb`, runs the API. Open the Aspire dashboard for URLs.

### Option B — manual Postgres

```bash
# 1. Start Postgres 16+ and set the connection string:
$env:ConnectionStrings__minibankdb = "Host=localhost;Port=5432;Database=minibankdb;Username=postgres;Password=postgres"

# 2. Run (Development auto-migrates + seeds admin + demo data):
dotnet run --project Backend/MiniBank.Api
```

Then:

- Scalar UI (Development): `http://localhost:5000/scalar`
- OpenAPI (all envs): `/openapi/v1.json`
- Health (all envs): `/health`, `/alive`
- Quick calls: [`Backend.http`](Backend.http) (VS / Rider / JetBrains HTTP client)

> Production never auto-migrates/seeds. Run `dotnet ef database update` (or your migrator) explicitly and configure `Cors:AllowedOrigins`.

### Seed & demo accounts

- Admin roles/users come from `Seed:Admin:Email/Password` or `ADMIN_EMAIL` / `ADMIN_PASSWORD` env vars (`AdminSeeder`, Development only). No env → nothing seeded, no crash.
- Demo data (`DemoSeeder`, Development only, idempotent): `demo@minibank.local` (5000 → transfers 250) and `sara@minibank.local` (2500), password `Demo123!`, references `demo-deposit-1/2`, `demo-transfer-1`.

## API quick tour

| Call | Notes |
|---|---|
| `POST /customers` | anonymous register (validates email/password 8+ with upper/lower/digit/special, phone 10–15 digits) |
| `POST /login` | Identity API → bearer token (use as `Authorization: Bearer …`) |
| `GET /customers`, `GET /accounts?page=&pageSize=` | self profile / paged accounts with balances (fail-fast `400` on bad paging, no silent clamp) |
| `POST /accounts/{id}/deposit`, `/withdraw` | `{ amount, idempotencyKey }` → `200` (replay included), `409` on key reuse with different payload, `422` on risk/limit |
| `POST /transfers` | `{ fromAccountId, toAccountId, amount, idempotencyKey }` — source must be owned + active |
| `GET /accounts/{id}/statement?page=&pageSize=` | ordered ledger entries |
| `POST /accounts` | → `201 Created` with `Location: /accounts/{id}/statement` |

### Status-code contract

State conflicts (frozen/closed/pending, wrong status, concurrency exhausted) → **`409 Conflict`**; business-rule violations (insufficient funds, daily limit, missing risk, KYC gate) → **`422 Unprocessable`**. `405` is never returned for domain errors (it means HTTP method mismatch).

### Idempotency semantics (global key)

`reference_id` has a **global** unique index (`ux_transactions_reference`). Replaying the same key with the **same** amount/type/accounts returns the original transaction (also across the check-then-insert race via the `23505 → return winner` path). Reusing a key with a **different** amount, type, or account(s) returns **`409 Conflict`** (`DomainConflictException`), never someone else's transaction. Empty/whitespace keys are `400`.

## Tests

```bash
dotnet build MiniBankSystem.sln -c Release
dotnet test MiniBankSystem.sln -c Release --no-build
# or per project: Backend.Tests/MiniBank.{Architecture,Domain,Features,Infrastructure,Api}.Tests
```

- `Domain.Tests` — pure aggregate/value-object unit tests
- `Features.Tests` — handler tests with mocked repos (incl. idempotent replay, risk `422`, compensation, admin `403`)
- `Infrastructure.Tests` — **real PostgreSQL via Testcontainers** (`postgres:18.6-alpine`) + real migrations (needs Docker)
- `Api.Tests` — `WebApplicationFactory` + EF InMemory + mocked mediator (verifies routing → command mapping + `Location` + status codes; `ResetMock` prevents stub leakage)
- `Architecture.Tests` — NetArchTest slice/DDD guardrails (included in the `.sln` and CI)

CI (`.github/workflows/ci.yml`): restore → build sln → 5 test steps, `concurrency.cancel-in-progress`. Infra tests use Testcontainers, so **no external postgres service** in CI.

## Configuration

| Key | Dev default | Prod |
|---|---|---|
| `ConnectionStrings:minibankdb` | via AppHost / env | required |
| `Cors:AllowedOrigins` | `localhost:3000,5173` | **required** (else startup throws) + `AllowCredentials` enabled |
| `Seed:Admin:Email/Password` or `ADMIN_EMAIL/PASSWORD` | empty → skip | set explicitly if needed |

Rate limits: `fixed` 100 auth / 20 anon per min (partitioned by `sub`/`NameIdentifier`, IP fallback; applied to all account/transfer endpoints), `auth_endpoints` 10/min/IP, `admin_endpoints` 50 auth / 5 anon per min (applied at `AdminController` level). Malformed `sub` claim → `401` (not `400`). No CSRF/antiforgery: bearer-token API. Rate limiting is bypassed in `Testing` for determinism.

## Known limitations (deliberate for a sample)

- Write path loads the full `Ledger` (`Include`) and recomputes balance in memory — `O(n)` per write. Reads use SQL `SUM`. A persisted balance snapshot is the obvious next step (see code comment in `AccountRepository`).
- Risk day windows use UTC dates; `Money` is USD-only with 2 decimals.
- Domain events dispatch out-of-band after `SaveChanges` (no outbox) — crash in between loses the event; audit logs are in-transaction so they survive.
- Concurrent opposite transfers (`A→B` + `B→A`) can deadlock (`40P01`); handlers load accounts in `Guid` order to reduce it and retry (max 3) on `ConcurrencyConflict`.
- Destinations must be `Active` to receive; blocked *customers* may still receive (banking convention, see `TransferHandler`).

## Layout

```
MiniBankSystem.sln (Backend + Backend.Tests + AppHost + ServiceDefaults; no Frontend project yet)
Backend/MiniBank.{Api,Features,Domain,Infrastructure,Abstractions}/
Backend.Tests/MiniBank.{Api,Architecture,Domain,Features,Infrastructure}.Tests/
MiniBank.AppHost/  MiniBank.ServiceDefaults/  Backend.http  .github/workflows/ci.yml
```

## License

MIT — see [LICENSE.txt](LICENSE.txt).
