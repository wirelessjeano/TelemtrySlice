# Solution folder structure

```
├── frontend/                    # Yarn monorepo (Turbo)
│   ├── apps/next/               # Next.js 14 app (standalone)
│   └── packages/
│       ├── trpc/                # tRPC routers + React clients
│       ├── domain/              # Shared TypeScript interfaces
│       └── tsconfig/            # Shared TS configs
├── backend/                     # .NET 8 solution
│   ├── TelemetrySlice.App.API/        # REST API (read-heavy)
│   ├── TelemetrySlice.App.Writer/     # RabbitMQ consumer (write-heavy)
│   ├── TelemetrySlice.Core/           # DI + extensions
│   ├── TelemetrySlice.Services/       # Business logic
│   ├── TelemetrySlice.Data/           # EF Core DbContext
│   ├── TelemetrySlice.Domain/         # Models + interfaces
│   ├── TelemetrySlice.Lib/            # Constants + utilities
│   └── TelemetrySlice.Tests/          # xUnit tests
├── data/                        # SQLite DB (shared volume)
└── docker-compose.yaml          # Orchestrates all services
```

# Operations and cloud path

## Running This in the Cloud

Swap each Docker Compose service for its managed equivalent. SQLite becomes InfluxDB (or InfluxDB Cloud) — it's purpose-built for time-series data like IoT telemetry, with built-in downsampling, retention policies, and efficient compression. Alternatively, Postgres (RDS or Azure SQL) works if you need a general-purpose relational store alongside the telemetry. RabbitMQ becomes Azure Service Bus or SQS, Redis becomes ElastiCache or Azure Cache. The API, Writer and Next.js frontend containers could run on Kubernetes (EKS/AKS), or a simpler option like ECS Fargate or Azure Container Apps if you don't need the full k8s feature set. Images live in ECR/ACR.  Wire everything together over a VNet/VPC so nothing's exposed to the public internet except the frontend and the ingestion endpoint.

## Isolating Customer Data

We already key everything by `CustomerId` — composite PKs on `Device (CustomerId, DeviceId)` and `TelemetryEvent (CustomerId, DeviceId, EventId)`, plus filtered queries throughout. Redis keys are prefixed too (`customer_exists:{customerId}`). Next step: extract a tenant ID from an auth token in middleware so every request is scoped automatically. If a customer ever needs hard isolation, give them their own database — or go further and spin up an entirely separate Kubernetes cluster per tenant using infrastructure as code (e.g. Terraform), so their workload is fully isolated at the infrastructure level. Row-level filtering covers most cases without the operational headache though.

## Handling Higher Volumes & Bursts

The Writer already uses a bounded channel (100k capacity, `BoundedChannelFullMode.Wait`) as backpressure between RabbitMQ and the DB. SQLite runs in WAL mode, so reads never block — the API can serve queries while the Writer is batching inserts. WAL only supports one writer though, so we keep a single Writer instance and scale it vertically. Add per-tenant rate limiting on the ingestion endpoint. Swapping SQLite for InfluxDB unlocks horizontal scaling of the Writer — it handles high-throughput concurrent writes natively, so we can spin up multiple Writer instances as competing consumers on RabbitMQ (that just works with the named queue setup). On Kubernetes, use Horizontal Pod Autoscaling to scale Writer replicas based on queue depth or CPU. If the cache layer gets hot, move to a Redis cluster.

## CI/CD

GitHub Actions with a branch-based workflow. Developers work on feature branches and merge into `development` via merge requests — the MR triggers lint + build the .NET projects and Next.js app, and runs tests. Merges to `development` also deploy straight to a staging environment for integration testing. When `development` is stable, it gets merged into `main`. On merge to `main`, the full pipeline kicks in: build Docker images, push to the container registry, run smoke tests, then promote to prod. Add SonarQube for static analysis and code quality gates, dependency scanning (Dependabot / `dotnet list package --vulnerable`), and container image scanning. Kubernetes rolling deployments give you zero-downtime rollouts — pods are updated in percentages, so there's always healthy capacity serving traffic during a deploy.

## Evolving the Data Model

The most impactful change is moving off SQLite to a database suited for time-series IoT data. InfluxDB is the natural choice — purpose-built for telemetry with retention policies, downsampling, and efficient storage. ClickHouse is another interesting option I'd like to explore — it's a columnar database that excels at analytical queries over large datasets, and its application to IoT workloads is worth investigating. For the relational side (customers, devices), we already use EF Core migrations. Keep changes backward-compatible: add nullable columns first, backfill data, then tighten constraints in a follow-up migration. Run migrations as a separate deploy step *before* rolling out new code so the old version still works against the updated schema. Always back up the DB before applying. CI should validate that migration scripts compile and apply cleanly against a disposable test database — catch broken migrations before they hit prod.

# Design

## Frontend

The frontend is a Yarn monorepo with Turbo, split into `apps/` and `packages/`:

- **@telemetryslice/next** (`apps/next`) — the Next.js 14 application. Handles routing, pages, and providers. Configured for standalone mode so it ships as a single container.
- **@telemetryslice/trpc** (`packages/trpc`) — all tRPC wiring lives here. Defines two routers: `appRouter` (customers, devices, metrics, charts, paginated telemetry) and `adminRouter` (health checks, seeding). Exports both server-side callers and React clients.
- **@telemetryslice/domain** (`packages/domain`) — shared TypeScript interfaces (`Customer`, `Device`, `TelemetryEvent`, `DeviceMetrics`, `DeviceChartItem`, `HealthReport`). Imported by both the tRPC package and the Next.js app, so types are defined once and used everywhere.
- **@telemetryslice/tsconfig** (`packages/tsconfig`) — shared TypeScript configs.

The key pattern is tRPC + React Query for end-to-end type safety. The `trpc` package uses `createTRPCReact` to generate typed React hooks (`appRouterClient`, `adminRouterClient`) backed by Tanstack Query. Components call hooks like `appRouterClient.deviceMetrics.useQuery(...)` and get full autocomplete and type checking from router to component — no manual API types, no code generation. Requests go through Next.js API routes (`/api/app/[trpc]`, `/api/admin/[trpc]`) which use `httpBatchLink` to batch multiple tRPC calls into a single HTTP request.

## Backend

The backend is a .NET 8 solution split into layers that each have a single responsibility. Domain defines the models and interfaces. Data handles database access with EF Core. Services contains the business logic. Core wires everything together with dependency injection. App is where the two hosts live. Each layer only depends on the ones below it, keeping things clean and testable. There are two separately deployed services:

- **API** (`TelemetrySlice.App.API`) — REST endpoints for querying customers, devices, and telemetry. Read-heavy.
- **Writer** (`TelemetrySlice.App.Writer`) — background worker that consumes telemetry from RabbitMQ and batch-writes to the database. Write-heavy.

This split exists because of SQLite's WAL (Write-Ahead Logging) mode. WAL lets many readers access the database concurrently without blocking, but only one writer can write at a time. Instead of having the API handle both reads and writes (and competing for the write lock), we dedicate a single Writer service to all database inserts, giving us high throughput without contention.

### Writer Flow

1. **`IncomingTelemetryService`** subscribes to RabbitMQ (topic exchange) and picks up `TelemetryEventMessage` events. RabbitMQ auto-ack is disabled — messages stay on the queue until we explicitly acknowledge them.
2. Each message is enqueued into the **`DatabaseWriterQueue`**, a bounded in-memory channel (100k capacity, `BoundedChannelFullMode.Wait`) that applies backpressure if the Writer falls behind.
3. **`DatabaseWriterService`** batch-reads up to 500 messages from the queue (with a 500ms flush timeout for partial batches). Before adding a message to the batch, it checks **Redis** for a dedup key (`{CustomerId}_{EventId}_{DeviceId}`) — if the key exists, the message is acked and skipped. This prevents duplicate telemetry from hitting the database.
4. The batch is inserted into SQLite via EF Core. On success, the dedup keys are cached in Redis with a 5-minute TTL, and the entire batch is acknowledged to RabbitMQ with a single `BasicAck(multiple: true)`.

Because the RabbitMQ ack only happens *after* a successful database insert, no messages are lost — if the Writer crashes mid-batch, unacked messages are redelivered by RabbitMQ automatically.

### Seed Function

The seed function generates 24 hours of telemetry data across multiple devices and publishes it through RabbitMQ — so it exercises the full ingestion pipeline, not just a direct database insert. The generated data intentionally includes real-world edge cases: ~1% of messages are **duplicate resends** (same EventId re-added to the stream), and ~5% of messages are **shuffled out of order** to simulate late delivery. This validates that the Redis deduplication layer correctly drops duplicates and that the Writer handles out-of-order arrivals gracefully.

### Tests

The backend has tests covering the key layers. The most important are the **`WriterPipelineTests`** — these test the full Writer flow end-to-end using an in-memory database and mocked RabbitMQ/Redis dependencies (via NSubstitute). They verify that messages flow from queue through the `DatabaseWriterService` to the database, that manual RabbitMQ acks fire only after a successful insert (with `multiple: true` on the highest delivery tag), and that Redis deduplication actually works. Specific scenarios covered: a single message persisted and acked, a duplicate message skipped by cache but still acked (so RabbitMQ doesn't redeliver), a batch of mixed new and duplicate messages where only the new ones are persisted and cached, and a batch of all duplicates where nothing is persisted and no cache writes happen.

The `EventTelemetryServiceTests` cover the persistence layer directly — batch inserts, deduplication by composite key, out-of-order arrivals preserving their original timestamps, and mixed scenarios combining duplicates with out-of-order messages. The `TelemetryServiceTests` cover the read/query side — chart data, paginated telemetry, and metric aggregations (latest, min, max, average, count).

## Trade-offs

### Duplicate Handling Outside the Cache TTL

The Redis dedup cache has a 5-minute TTL. If a duplicate message arrives after that window, it won't be caught by the cache and will end up in a batch. When `SaveBatchAsync` tries to insert the batch, SQLite throws a key violation. The error handling for this falls back to looping through every item in the batch and inserting them one at a time — the duplicate gets caught and skipped, and the rest of the batch is saved. It works, but inserting row-by-row isn't great for performance and there is a performance penalty here. 

An alternative would be to identify the offending duplicate, remove it from the batch, and retry the batch insert — but that adds complexity for a case that should be rare in practice. The current approach trades some performance in the edge case for simpler, more predictable error handling.

The TTL on the dedup cache is intentional — without it, Redis would eventually run out of memory as keys accumulate indefinitely. The TTL just needs to be longer than the IoT device's retry/buffer window. If a device buffers unsent messages for up to 2 minutes before resending, a 5-minute TTL gives plenty of headroom to catch those duplicates while keeping memory usage bounded.