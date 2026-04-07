# CLAUDE.md

This file provides guidance to Claude Code when working with this repository.

## What is SharpStreamer

SharpStreamer is a .NET 10 library implementing the **Transactional Outbox/Inbox Pattern** for reliable, ordered event communication between microservices. Events are inserted into the outbox table within the caller's DB transaction, then background services pick them up, publish via a transport, store in the inbox, and process via MediatR handlers.

**Supported combinations:**
- **Transports (message brokers):** RabbitMQ, Kafka, PostgreSQL (loopback), SQLite (loopback)
- **Storage backends:** PostgreSQL (Npgsql), SQLite

All projects target `net10.0`. The recommended production combination is **RabbitMQ transport + Npgsql storage**.

## Build & Test Commands

```bash
# Build entire solution
dotnet build
dotnet build --configuration Release

# Run Npgsql tests (requires Docker — uses Testcontainers for PostgreSQL)
dotnet test tests/Storage.Npgsql.Tests/Storage.Npgsql.Tests.csproj

# Run SQLite tests (no Docker required — uses temp file DB)
dotnet test tests/Storage.Sqlite.Tests/Storage.Sqlite.Tests.csproj

# Run a single test by name
dotnet test tests/Storage.Sqlite.Tests/Storage.Sqlite.Tests.csproj --filter "FullyQualifiedName~MethodName"
```

## Publishing NuGet Packages

The helper script `PublishProjectScript.cs` (top-level statements, `net10.0`) auto-reads `<Version>` from the `.csproj`, packs in Release mode, and pushes:

```bash
dotnet run .\PublishProjectScript.cs -- <PROJECT_NAME> <NUGET_API_KEY>
# Example:
dotnet run .\PublishProjectScript.cs -- DotNetCore.SharpStreamer.Transport.RabbitMq YOUR_KEY
```

**Remember to bump `<Version>` in the target `.csproj` before publishing.**

## Project Layout

```
src/
  DotNetCore.SharpStreamer/                    # Core: interfaces, entities, attributes, DI, caching
  DotNetCore.SharpStreamer.Storage.Npgsql/     # EF Core + PostgreSQL storage (schema: sharp_streamer)
  DotNetCore.SharpStreamer.Storage.Sqlite/     # EF Core + SQLite storage (no schema prefix)
  DotNetCore.SharpStreamer.Transport.Npgsql/   # PostgreSQL loopback transport
  DotNetCore.SharpStreamer.Transport.Sqlite/   # SQLite loopback transport
  DotNetCore.SharpStreamer.Transport.Kafka/    # Kafka transport
  DotNetCore.SharpStreamer.Transport.RabbitMq/ # RabbitMQ transport
samples/
  DotNetCore.SharpStreamer.RabbitMq.Npgsql/    # RabbitMQ + Npgsql sample
  DotNetCore.SharpStreamer.Kafka.Npgsql/       # Kafka + Npgsql sample
  DotNetCore.SharpStreamer.Npgsql.Npgsql/      # Npgsql loopback sample
  DotNetCore.SharpStreamer.Sqlite.Sqlite/      # SQLite loopback sample
tests/
  Storage.Npgsql.Tests/                         # Integration tests (Testcontainers + PostgreSQL)
  Storage.Sqlite.Tests/                         # Integration tests (temp file SQLite)
```

## Current Package Versions

| Package | Version |
|---|---|
| DotNetCore.SharpStreamer (Core) | 4.0.2 |
| Storage.Npgsql | 4.0.1 |
| Storage.Sqlite | 1.0.1 |
| Transport.RabbitMq | 3.0.3 |
| Transport.Kafka | (no version tag) |
| Transport.Npgsql | 4.0.1 |
| Transport.Sqlite | 1.0.1 |

## Core Architecture

### DI Registration Order (Critical)

MediatR **must** be registered before SharpStreamer, then storage, then transport:

```csharp
builder.Services.AddMediatR(options =>
{
    options.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
    options.Lifetime = ServiceLifetime.Transient;
});

builder.Services
    .AddSharpStreamer("SharpStreamerSettings", Assembly.GetExecutingAssembly())
    .AddSharpStreamerStorageNpgsql<YourDbContext>()   // or AddSharpStreamerStorageSqlite<T>()
    .AddSharpStreamerTransportRabbitMq();             // or Kafka/Npgsql/Sqlite
```

`AddSharpStreamer` throws `InvalidOperationException` if `IMediator` is not already registered. The first parameter is the config section name (e.g., `"SharpStreamerSettings"`), and the assemblies are scanned for `[ConsumeEvent]`-attributed types that implement `IRequest`.

### Key Interfaces and Types

**`IStreamerBus`** (`Bus/IStreamerBus.cs`) — the main publishing interface, registered as scoped:
- `PublishAsync<T>(T message, string eventKey, params KeyValuePair<string, string>[] headers)` — stores event in outbox within the caller's DB transaction. The `eventKey` controls ordering.
- `PublishDelayedAsync<T>(T message, TimeSpan delay, params KeyValuePair<string, string>[] headers)` — schedules event with a delay. Uses a random GUID v7 as `eventKey` (no ordering guarantee). The delay is implemented by setting `SentAt = now + delay`; the publisher background service only picks events where `SentAt < now`.

**`Event<TId>`** (`Entities/Abstractions/Event.cs`) — abstract base with `Id`, `Topic`, `Content` (JSON string), `RetryCount`, `SentAt`, `Timestamp`, `Status`, `EventKey`.

**`PublishedEvent : Event<Guid>`** — outbox table entity, no extra properties.

**`ReceivedEvent : Event<Guid>`** — inbox table entity with additional: `Group`, `ErrorMessage`, `UpdateTimestamp`, `Partition`, `NextExecutionTimestamp`.

**`EventStatus` enum:** `None = 0`, `InProgress = 1`, `Succeeded = 2`, `Failed = 3`.

### Attributes

**`[PublishEvent(eventName, topicName)]`** — on event DTO classes. `topicName` maps to the broker topic/exchange. `eventName` is stored inside the JSON `Content` under `"event_name"`.

**`[ConsumeEvent(eventName, checkPredecessor = true)]`** — on handler DTO classes (must also implement `IRequest`). `checkPredecessor` controls whether the processor verifies all prior events with the same `eventKey` are `Succeeded` before processing.

A single class can have both attributes (e.g., in loopback scenarios where the same service publishes and consumes).

### Event Content Format

Events are serialized as JSON with this structure:
```json
{
  "body": { /* the actual event DTO */ },
  "event_name": "user_created",
  // ... any custom headers passed via KeyValuePair[]
}
```

The processor extracts `body` and `event_name`, looks up the consumer metadata by `event_name`, deserializes `body` into the handler type, and dispatches via `IMediator.Send()`.

### Event Ordering via `eventKey`

- Same `eventKey` → **serial processing** (each event checks predecessors by `Timestamp` order)
- Different `eventKey` → **eligible for parallel processing**
- `Failed` events **block** all subsequent events with the same key until resolved
- Predecessor check queries: `WHERE EventKey = X AND Status IN (Failed, None, InProgress) AND Timestamp < current`

### ID Generation

Uses `Guid.CreateVersion7()` (via `UlidGenerator : IIdGenerator`) — monotonically increasing GUIDs that sort chronologically.

### Time Service

`TimeService` wraps `TimeProvider.System` (registered as `TryAddSingleton`), enabling test-time substitution. `ITimeService.GetUtcNow()` and `ITimeService.Delay()` are used throughout.

## Storage Layer Details

Both Npgsql and SQLite storage projects follow the same structure. Each registers these services:

| Service | Lifetime | Purpose |
|---|---|---|
| `StreamerBus<TDbContext> : IStreamerBus` | Scoped | Inserts into outbox via raw SQL |
| `EventsRepository<TDbContext> : IEventsRepository` | Scoped | All DB queries via `SqlQueryRaw`/`ExecuteSqlRawAsync` |
| `EventsProcessor` | BackgroundService | Fetches received events, processes via MediatR |
| `EventsPublisher` | BackgroundService | Fetches published events, sends via `ITransportService` |
| `ProcessedEventsCleaner` | BackgroundService | Deletes `Succeeded` received events older than 1 day |
| `ProducedEventsCleaner` | BackgroundService | Deletes `Succeeded` published events older than 1 day |
| `MigrationService<TDbContext> : IMigrationService` | Singleton | Runs EF Core migrations once at startup (double-checked locking) |
| `IDistributedLockProvider` | Singleton | Npgsql: `PostgresDistributedSynchronizationProvider`; SQLite: `FileSystemDistributedSynchronizationProvider` |
| `IEventsProcessor` / `IEventProcessor` | Scoped | Internal processing orchestration |

### Database Tables

**Npgsql:** Tables in `sharp_streamer` schema — `sharp_streamer.published_events`, `sharp_streamer.received_events`.

**SQLite:** No schema prefix — `published_events`, `received_events` directly.

Both use EF Core migrations. Generate with:
```bash
# Npgsql
dotnet ef migrations add <Name> --project src/DotNetCore.SharpStreamer.Storage.Npgsql

# SQLite
dotnet ef migrations add <Name> --project src/DotNetCore.SharpStreamer.Storage.Sqlite
```

### Key Database Indexes

- `IX_Events_For_Processing_New` — `(Status, NextExecutionTimestamp, RetryCount)` — used by `GetAndMarkEventsForProcessing`
- `IX_EventKey_Status` — `(EventKey, Status, Timestamp)` filtered on Status 0/1/3 — used for predecessor checks
- `IX_Events_For_Publishing` — `(Status, SentAt)` — used by publisher
- `IX_ReceivedEvents_Timestamp` / `IX_PublishedEvents_Timestamp` — BRIN indexes on Npgsql, regular on SQLite

### Processing Flow

1. `EventsProcessor` (BackgroundService) runs `ProcessorThreadCount` parallel tasks, each polling every **1 second**.
2. `EventsProcessorService.ProcessEvents()` acquires a distributed lock (`{ConsumerGroup}-EventsProcessorService`, 2 min timeout), fetches a batch of events where `Status IN (None, Failed) AND NextExecutionTimestamp < now AND RetryCount < 50`, marks them `InProgress` with `RetryCount++`.
3. For each event sequentially: `EventProcessorService.ProcessEvent()` acquires a per-eventKey lock (`{ConsumerGroup}-{eventKey}`, `ProcessingTimeoutMinutes + 2` min timeout), optionally checks predecessors, deserializes, and dispatches via `IMediator.Send()` with a `CancellationTokenSource` timeout of `ProcessingTimeoutMinutes`.
4. Post-processing: sets `Status`, `ErrorMessage` (truncated to 1000 chars, single quotes replaced with dashes), and `NextExecutionTimestamp` (now + 20 seconds for retry backoff).

### Publishing Flow

1. `EventsPublisher` (BackgroundService) runs `ProcessorThreadCount` parallel tasks, each polling every **2 seconds**.
2. Acquires lock `{ConsumerGroup}-EventsPublisher` (10 min timeout), fetches events where `Status = None AND SentAt < now`, increments `RetryCount`, sends via `ITransportService.Publish()`, then marks as `Succeeded`.

### Cleaner Jobs

Both cleaners run every **1 second**, acquire their own distributed locks (2 min timeout), and delete `Succeeded` events older than **1 day**.

### SQLite vs Npgsql Differences

- No `sharp_streamer.` schema prefix in SQL queries
- Uses `IN ({0}, {1}, ...)` with `BuildInClauseParams` helper instead of PostgreSQL `ANY()` arrays
- Content column type is `TEXT` instead of `json`
- No BRIN indexes (regular B-tree instead)
- Distributed locking via `DistributedLock.FileSystem` instead of `DistributedLock.Postgres` — lock directory is `.sharp_streamer_locks` next to the DB file (or `sharp_streamer_locks` in temp for in-memory databases)

## Transport Layer Details

### RabbitMQ Transport

**Config section:** `SharpStreamerSettings:RabbitMq`

```json
{
  "RabbitMq": {
    "Topics": ["identity", "payments"],
    "ConnectionSettings": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest"
    }
  }
}
```

- `RabbitOptions`: `Topics` (list of exchange names to declare), `ConnectionSettings` (RabbitMQ.Client `ConnectionFactory`).
- `SetupRabbitInfrastructureHostedService`: On startup, declares fanout exchanges for each topic and binds the queue (named after `ConsumerGroup`) to each exchange. **Stops the application on failure.**
- `RabbitConnectionProvider`: Singleton, lazy connection with semaphore. Auto-configures `AutomaticRecoveryEnabled = true`, `TopologyRecoveryEnabled = true`, `NetworkRecoveryInterval = 10s`, `RequestedHeartbeat = 30s`.
- `RabbitConsumer`: Uses `x-single-active-consumer` queue argument for ordered delivery per queue. `prefetchCount = 1`. Extracts `Id`, `SentAt` from message headers (UTF-8 byte arrays), uses `RoutingKey` as `EventKey`. **Stops the application on unrecoverable error.**
- `RabbitTransportService`: Publishes with `ExchangeType.Fanout`, `Persistent = true`, `ContentType = application/json`, `ContentEncoding = utf-8`. `Id` and `SentAt` are sent as UTF-8 byte headers. Uses `EventKey` as routing key.

### Kafka Transport

**Config section:** `SharpStreamerSettings:Kafka`

```json
{
  "Kafka": {
    "Servers": "localhost:9092",
    "TopicsToBeConsumed": ["identity", "payments"],
    "CommitBatchSize": 100,
    "CommitTimespanSeconds": 5,
    "ProducersCount": 3
  }
}
```

- `KafkaConsumer`: Runs `ConsumerThreadCount` parallel consumers. Uses `AutoOffsetReset.Latest`, `EnableAutoCommit = false`. Commits after `CommitBatchSize` messages or `CommitTimespanSeconds` elapsed. Extracts `Id`/`SentAt` from Kafka headers. Uses `Message.Key` as `EventKey`.
- `KafkaTransportService`: Creates `ProducersCount` producers with `Acks.All`, `EnableIdempotence = true`, `LingerMs = 50`. Groups events by `EventKey`, distributes across producers round-robin, publishes in order within each key group.
- **Topics must be pre-created manually** — Kafka transport does not auto-create topics.

### Npgsql/SQLite Loopback Transports

These convert published events directly to received events in the same database (no external broker). They copy `Id`, `Content`, `Topic`, `EventKey` from the published event and set incrementing timestamps (`AddMilliseconds(index)`) to preserve ordering within a batch. Useful for single-service or testing scenarios.

## Configuration Reference

```json
{
  "SharpStreamerSettings": {
    "Core": {
      "ConsumerGroup": "UniqueServiceName",
      "ProcessorThreadCount": 5,
      "ProcessingBatchSize": 1000,
      "ProcessingTimeoutMinutes": 8,
      "ConsumerThreadCount": 5
    }
  }
}
```

| Field | Required | Description |
|---|---|---|
| `ConsumerGroup` | Yes | Unique per microservice. Used as queue name (RabbitMQ), consumer group (Kafka), and lock key prefix. |
| `ProcessorThreadCount` | Yes | Number of parallel processing tasks for both `EventsProcessor` and `EventsPublisher`. |
| `ProcessingBatchSize` | Yes | Max events fetched per processing/publishing cycle. |
| `ProcessingTimeoutMinutes` | Yes | CancellationToken timeout for event handler execution. Lock timeout = this + 2 minutes. |
| `ConsumerThreadCount` | For Kafka/RabbitMQ | Number of consumer threads. Not used by loopback transports. Kafka throws if 0. |

## InternalsVisibleTo Map

The `internal` access modifiers are bridged via `[InternalsVisibleTo]`:

| Declared in | Visible to |
|---|---|
| `ICacheService.cs` (Core) | `Storage.Npgsql`, `Storage.Sqlite` |
| `SharpStreamerExtensions.cs` (Core) | `Storage.Npgsql`, `Transport.RabbitMq` |
| `DiService.cs` (Core) | `Transport.Kafka` |
| `SharpStreamerEfCoreNpgsqlExtensions.cs` | `Storage.Npgsql.Tests` |
| `EventsProcessor.cs` (Npgsql) | `Storage.Npgsql.Tests`, `DynamicProxyGenAssembly2` |
| `SharpStreamerEfCoreSqliteExtensions.cs` | `Storage.Sqlite.Tests` |

New storage projects need to be added to `ICacheService.cs`'s `InternalsVisibleTo`.

## Important Constraints and Gotchas

- **Retry count is hardcoded at 50** — events with `RetryCount >= 50` are never picked up again
- **NextExecutionTimestamp** controls retry backoff — set to `now + 20 seconds` after each failed processing attempt
- **Error messages** are truncated to 1000 characters and single quotes (`'`) are replaced with dashes (`-`) to avoid SQL issues
- **Cleaners delete after 1 day** — `Succeeded` events older than 24 hours are permanently deleted
- **EventsPublisher lock timeout is hardcoded at 10 minutes**, with a 5-minute `CancellationTokenSource` for the transport publish call
- **Kafka topics must be created manually** before service starts
- **RabbitMQ consumer/infrastructure errors stop the application** via `IHostApplicationLifetime.StopApplication()`
- **Migration runs once** — `MigrationService` uses double-checked locking; the first `StreamerBus` or `EventsRepository` resolution triggers it
- **DbContext for SharpStreamer tables** — the library has its own internal `NpgsqlDbContext` / `SqliteDbContext` that calls `ConfigureSharpStreamerNpgsql()` / `ConfigureSharpStreamerSqlite()` for migrations. The user's DbContext should NOT call these methods — it only needs `modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())` for its own entities. The user's DbContext is used by the library for raw SQL operations.
- **Content is stored as JSON** — `json` column type on PostgreSQL, `TEXT` on SQLite
- **Loopback transports** add incrementing milliseconds to `Timestamp` to preserve ordering within a batch

## Testing Patterns

- **Framework:** xUnit with `IClassFixture`, `ICollectionFixture`, `IAsyncLifetime`
- **Mocking:** NSubstitute
- **Assertions:** FluentAssertions
- **Test data:** AutoFixture
- **Npgsql tests:** `Testcontainers.PostgreSql` for real PostgreSQL in Docker
- **SQLite tests:** Temp file DB (`Path.GetTempPath() + random GUID`), cleaned up on dispose with `SqliteConnection.ClearAllPools()`
- **Test base class:** `DatabaseTest` — provides `DbContext`, clears `ReceivedEvent`/`PublishedEvent` tables between tests
- **Tests cover:** Repository CRUD operations, processing flow (lock acquisition, predecessor checking, status transitions, error message truncation, processed events dictionary tracking), batch size limits

## Key Dependencies

| Package | Used by |
|---|---|
| MediatR 12.5.0 | Core |
| Microsoft.Extensions.* 9.0.9 | Core |
| Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1 | Storage.Npgsql |
| Microsoft.EntityFrameworkCore.Sqlite 10.0.5 | Storage.Sqlite |
| DistributedLock.Postgres 1.3.0 | Storage.Npgsql |
| DistributedLock.FileSystem 1.0.3 | Storage.Sqlite |
| RabbitMQ.Client 7.1.2 | Transport.RabbitMq |
| Confluent.Kafka 2.12.0 | Transport.Kafka |
| Testcontainers.PostgreSql 4.8.1 | Npgsql Tests |
| FluentAssertions 8.8.0 | Tests |
| NSubstitute 5.3.0 | Tests |
| AutoFixture 4.18.1 | Tests |
