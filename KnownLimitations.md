# Known Limitations

## Contents

1. [Not implemented yet](#1-not-implemented-yet)
2. [Security and identity](#2-security-and-identity)
3. [Concurrency](#3-concurrency)
4. [Domain workflows](#4-domain-workflows)
5. [API and data](#5-api-and-data)
6. [Tooling and tests](#6-tooling-and-tests)

---

## 1. Not implemented yet

- **End-user authentication.** Only service-to-service authentication exists right now. Plan: separate cookie-session
  BFFs for the admin panel and the storefront, forwarding user access tokens to services.
- **Ordering and ticketing.** No seat holds, orders, payments or tickets yet. EventService already publishes the price
  messages (`EventSeatCategoryPrice*`, `EventCancelled`, `EventDecisionRequired`), but nothing consumes them yet.
- **Notifications, check-in, storefront app.** Planned - see the status table in the [README](README.md#status).
- **Admin app.** App shell and basic CRUD only so far.

---

## 2. Security and identity

### The Gateway doesn't authenticate end users yet

For every proxied route, the Gateway replaces the `Authorization` header with its own client-credentials token
([`Gateway/Program.cs`](src/backend/TixTapGo.Gateway/Program.cs)) instead of checking who's actually calling. That
means anyone who can reach the Gateway can call any VenueService or EventService endpoint - fine for local dev, not
for anything beyond it.

This isn't something to patch on the Gateway alone; it needs the end-user identity work to land first. Once it does:
add token validation and scope-based policies at the Gateway as a coarse edge filter, forward user tokens from the
BFFs instead of overwriting them, and widen each service's `InternalOnly` policy to "known client **or** authenticated
user with the required scope".

### Tokens carry no scopes or audiences

Client apps only get token-endpoint and client-credentials permissions today, and services authorize purely on `sub`
(which is just the client id). Coarse, but consistent for now. Scopes (`admin`, `shop`) should land together with the
BFF client registrations.

### Idempotency keys aren't scoped per caller

The Redis key is built from path + method + the client-supplied `Idempotency-Key`
([`IdempotencyStateManager`](src/backend/TixTapGo.Shared.Persistence/DAL/Idempotency/IdempotencyStateManager.cs)), so
two callers presenting the same key - copied from sample code, a buggy client, a leaked log - would get each other's
cached response. The real concern is authorization scoping, not collision odds; Stripe scopes keys by account for the
same reason.

No per-user identity reaches the services yet, and every request currently arrives as the Gateway, so scoping by the
current principal wouldn't change anything today. Once user identity flows downstream, adding the caller's id to the
cache key is a one-line fix.

### A few settings are development-only by design

AuthService always uses OpenIddict's **development** signing and encryption certificates, `/health` and `/alive` are
mapped only in Development, and the `client-credentials-encryption-key` AppHost parameter isn't persisted (unlike
client secrets). Maybe I'll add keycloak some day. All of this needs environment-specific configuration once an actual deployment target exists.

---

## 3. Concurrency

### Concurrent writes can over-assign category capacity

Creating or enabling a price checks remaining capacity with a read, then writes in a separate step - so two
concurrent requests can both see `remaining = 10`, both assign 8, and both succeed. The `UpdatedAt` concurrency token
doesn't catch this, because nothing *updates* the same row.
([`SeatCategoryPriceEndpoints`](src/backend/TixTapGo.EventService/Endpoints/EventSeatCategoryPrice/SeatCategoryPriceEndpoints.cs))

This can't actually bite until real purchase traffic exists - the in-app pre-checks already cover sequential requests,
which is everything today. The eventual fix is a PostgreSQL trigger as a safety net behind those checks: lock the
category row (`FOR NO KEY UPDATE`) before summing, since under `READ COMMITTED` a trigger that doesn't lock still lets
both inserts through; cover every write path (price insert/update, category capacity shrink, an event re-entering
`Upcoming`); and map the raised exception to the same `400` Problem response as the pre-check, so race losers don't
get a `500`.

### Event issues are a plain `jsonb` instead of an owned/complex collection

`Event.ActiveIssues` is a scalar `jsonb` column with a value converter
([`EventConfiguration`](src/backend/TixTapGo.EventService/DAL/Configurations/EventConfiguration.cs)). I tried the complex
type first, but it is limited in which collection types it supports (`IReadOnlyList` isn't supported, surprisingly),
and `ReloadAsync()` throws `InvalidOperationException` ([dotnet/efcore#38632](https://github.com/dotnet/efcore/issues/38632),
still open), which broke the `DbUpdateConcurrencyException` processing. The owned type didn't help either because
ReloadAsync just doesn't reload them from the DB, and EF doesn't mark the owning entity's State as Modified when a new
item is added to its owned collection.

This change introduced a non-obvious consequence. Npgsql can't translate `.Count` or `.Any()` on a value-converted
collection, so an empty list is saved as SQL `NULL` and "has issues" becomes an `IS NOT NULL` check, for example in
[`CancelUnresolvedEventsJob`](src/backend/TixTapGo.EventService/Jobs/CancelUnresolvedEventsJob.cs). Also `GET /events`
loads entities and maps them in memory instead of projecting in SQL.

---

## 4. Domain workflows

### Resolving an event issue isn't wired up yet

When a venue is deleted, or a category is removed or shrunk, the event records an issue and the affected prices get
disabled with `ResolutionRequired = true`. Nothing clears `ResolutionRequired` or offers a way to actually resolve it
(reassign a venue, adjust capacity, re-point a price).

### No standing-room or mixed admission

A venue seat category has no capacity of its own - it's just the number of seats mapped to it. So there's no way to
model standing capacity (say, a zone certified for 50 people with 20 seats), or to sell one zone as both reserved
seats and general admission within the same event.

---

## 5. API and data

- List endpoints aren't paginated (`GET /events`, `/venues`, `/venues/{id}/seating-maps/{v}/seats`, category prices), and a seating map can hold tens of thousands of seats
- The synchronous `SaveChanges()` skips `DomainEventsInterceptor`, so it doesn't publish domain events - only
  `SaveChangesAsync()` does. It still stamps audit columns and cascades soft delete, through a sync-over-async call
  that's reason enough to drop the override

---

## 6. Tooling and tests

- Test coverage is basically just shared infrastructure right now - integration tests cover soft delete and idempotency,
nothing yet covers pricing/capacity rules, endpoints, or message consumers.
- No CI pipeline yet - planned as a GitHub Actions workflow running the backend build/tests (Testcontainers) plus
client apps lint and build.
