# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is SharpStreamer

SharpStreamer is a .NET library implementing the **Transactional Outbox/Inbox Pattern** for reliable, ordered event communication between microservices. It supports RabbitMQ, Kafka, PostgreSQL, and SQLite as message brokers/transports, with PostgreSQL (Npgsql) and SQLite as storage backends.

## Build & Test Commands

```bash
# Build
dotnet build
dotnet build --configuration Release

# Run Npgsql tests (requires Docker for PostgreSQL via Testcontainers)
dotnet test tests/Storage.Npgsql.Tests/Storage.Npgsql.Tests.csproj

# Run SQLite tests (no Docker required)
dotnet test tests/Storage.Sqlite.Tests/Storage.Sqlite.Tests.csproj

# Run a single test
dotnet test tests/Storage.Sqlite.Tests/Storage.Sqlite.Tests.csproj --filter "FullyQualifiedName~MethodName"
```

## Publishing NuGet Packages

The helper script `PublishProjectScript.cs` auto-reads the version from the `.csproj` file, packs, and pushes:
```bash
dotnet run .\PublishProjectScript.cs -- <PROJECT_NAME> <NUGET_API_KEY>
# Example: dotnet run .\PublishProjectScript.cs -- DotNetCore.SharpStreamer.Transport.RabbitMq YOUR_KEY
```

Manual alternative:
```bash
# 1. Navigate into the class library project directory
# 2. Pack in release mode
dotnet pack --configuration Release
# 3. Push (the script reads version from csproj automatically)
dotnet nuget push .\bin\Release\<PackageName>.<Version>.nupkg -s https://api.nuget.org/v3/index.json -k <YOUR_API_KEY>
```

Remember to update the `<Version>` in the `.csproj` file before publishing a new version.

## Project Layout

```
src/
  DotNetCore.SharpStreamer/                    # Core abstractions (IStreamerBus, Event<T>, attributes)
  DotNetCore.SharpStreamer.Storage.Npgsql/     # EF Core + PostgreSQL storage (Outbox/Inbox tables)
  DotNetCore.SharpStreamer.Storage.Sqlite/     # EF Core + SQLite storage (Outbox/Inbox tables)
  DotNetCore.SharpStreamer.Transport.Npgsql/   # PostgreSQL as message broker (loopback)
  DotNetCore.SharpStreamer.Transport.Sqlite/   # SQLite as message broker (loopback)
  DotNetCore.SharpStreamer.Transport.Kafka/    # Kafka transport
  DotNetCore.SharpStreamer.Transport.RabbitMq/ # RabbitMQ transport (most stable)
samples/                                        # One sample per broker/storage combination
tests/
  Storage.Npgsql.Tests/                         # XUnit integration tests (Testcontainers + PostgreSQL)
  Storage.Sqlite.Tests/                         # XUnit integration tests (temp file SQLite, no Docker)
```

## Core Architecture

### Key Abstractions (Core project)

- **`IStreamerBus`** — Main interface. `PublishAsync<T>()` stores the event in the outbox within the current DB transaction. `PublishDelayedAsync<T>()` schedules events with a delay using a random GUID as the eventKey (no ordering guarantee).
- **`Event<TId>`** — Base class. `PublishedEvent` and `ReceivedEvent` are the concrete types stored in `published_events` and `received_events` tables.
- **`[PublishEvent(eventName, topicName)]`** — Attribute on event DTOs marking the broker topic and event type.
- **`[ConsumeEvent(eventName, checkPredecessor)]`** — Attribute on handler DTOs marking what events they handle.

### Event Ordering via `eventKey`

The `eventKey` parameter is the ordering unit:
- Same `eventKey` → serial processing (each event waits for the previous one)
- Different `eventKey` → eligible for parallel processing
- Events with `Failed` status **block** all subsequent events with the same key — must be resolved manually

### Storage Layer

Each storage implementation provides the same set of components:

- **`StreamerBus<TDbContext>`** — EF Core implementation of `IStreamerBus`; scoped lifetime, tied to the caller's `DbContext`
- **`EventsRepository<TDbContext>`** — All DB queries via raw SQL (`SqlQueryRaw`/`ExecuteSqlRawAsync`)
- **Background hosted services:** `EventsPublisher`, `EventsProcessor`, `ProcessedEventsCleaner`, `ProducedEventsCleaner`
- **Distributed locking:** Npgsql uses `DistributedLock.Postgres`; SQLite uses `DistributedLock.FileSystem`

**SQLite-specific differences from Npgsql:**
- No schema prefix (tables are `published_events`/`received_events` directly)
- Uses EF Core migrations (same as Npgsql) — generate with `dotnet ef migrations add <Name> --project src/DotNetCore.SharpStreamer.Storage.Sqlite`
- Parameterized `IN (...)` clauses instead of PostgreSQL `ANY()` arrays
- Batch `MarkPostProcessing` uses individual UPDATE statements instead of CASE (Guid format mismatch with inline SQL literals in SQLite)

### Transport Layer

Each transport implements a consumer and a transport service:
- **RabbitMQ**: `RabbitConsumer` uses Single Active Consumer (SAC) for ordered delivery per queue; auto-creates queues and exchange bindings
- **Kafka**: `KafkaConsumer` relies on partition-based ordering; topics must be **pre-created manually**
- **Npgsql/Sqlite**: Loopback transports — converts published events to received events in the same database

### MediatR Integration

SharpStreamer dispatches received events through MediatR. **MediatR must be registered before SharpStreamer** in `Program.cs`:

```csharp
builder.Services.AddMediatR(...);
builder.Services.AddSharpStreamer(...);
```

## Configuration Reference

```json
{
  "SharpStreamerSettings": {
    "Core": {
      "ConsumerGroup": "UniqueServiceName",      // required, unique per microservice
      "ProcessorThreadCount": 5,               // required
      "ProcessingBatchSize": 1000,             // required
      "ProcessingTimeoutMinutes": 8,           // required; lock timeout = this + 2 minutes
      "ConsumerThreadCount": 5                 // required for Kafka/RabbitMQ
    }
  }
}
```

## Important Constraints

- Retry count is fixed at **50** — not configurable
- Kafka topics must be created manually before the service starts
- The recommended stable combination is **RabbitMQ transport + Npgsql storage**
- `ProcessingTimeoutMinutes` controls how long a single event handler can run; the distributed lock timeout is always `ProcessingTimeoutMinutes + 2`
- `ICacheService` is `internal` — new storage projects need `InternalsVisibleTo` in `ICacheService.cs`
