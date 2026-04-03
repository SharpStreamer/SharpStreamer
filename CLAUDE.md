# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is SharpStreamer

SharpStreamer is a .NET library implementing the **Transactional Outbox/Inbox Pattern** for reliable, ordered event communication between microservices. It supports RabbitMQ, Kafka, and PostgreSQL as message brokers, with PostgreSQL (Npgsql) as the storage backend.

## Build & Test Commands

```bash
# Build
dotnet build
dotnet build --configuration Release

# Run all tests (requires Docker for PostgreSQL via Testcontainers)
dotnet test tests/Storage.Npgsql.Tests/Storage.Npgsql.Tests.csproj

# Run a single test
dotnet test tests/Storage.Npgsql.Tests/Storage.Npgsql.Tests.csproj --filter "FullyQualifiedName~MethodName"

# Pack NuGet packages
dotnet pack src/<ProjectName>/<ProjectName>.csproj --configuration Release

# Publish to NuGet (uses the helper script)
dotnet run PublishProjectScript.cs -- <ProjectName> <NUGET_API_KEY>
```

## Project Layout

```
src/
  DotNetCore.SharpStreamer/                   # Core abstractions (IStreamerBus, Event<T>, attributes)
  DotNetCore.SharpStreamer.Storage.Npgsql/    # EF Core + PostgreSQL storage (Outbox/Inbox tables)
  DotNetCore.SharpStreamer.Transport.Npgsql/  # PostgreSQL as message broker
  DotNetCore.SharpStreamer.Transport.Kafka/   # Kafka transport
  DotNetCore.SharpStreamer.Transport.RabbitMq/ # RabbitMQ transport (most stable)
samples/                                       # One sample per broker/storage combination
tests/Storage.Npgsql.Tests/                    # XUnit integration tests (Testcontainers + PostgreSQL)
```

## Core Architecture

### Key Abstractions (Core project)

- **`IStreamerBus`** — Main interface. `PublishAsync<T>()` stores the event in the outbox within the current DB transaction. `PublishDelayedAsync<T>()` schedules events with a delay using a random GUID as the eventKey (no ordering guarantee).
- **`Event<TId>`** — Base class. `PublishedEvent` and `ReceivedEvent` are the concrete types stored in `sharp_streamer.published_events` and `sharp_streamer.received_events`.
- **`[PublishEvent(eventName, topicName)]`** — Attribute on event DTOs marking the broker topic and event type.
- **`[ConsumeEvent(eventName, checkPredecessor)]`** — Attribute on handler DTOs marking what events they handle.

### Event Ordering via `eventKey`

The `eventKey` parameter is the ordering unit:
- Same `eventKey` → serial processing (each event waits for the previous one)
- Different `eventKey` → eligible for parallel processing
- Events with `Failed` status **block** all subsequent events with the same key — must be resolved manually

### Storage Layer (Npgsql project)

- **`StreamerBusNpgsql<TDbContext>`** — EF Core implementation of `IStreamerBus`; scoped lifetime, tied to the caller's `DbContext`
- **`EventsRepository<TDbContext>`** — All DB queries (uses Dapper for reads, EF for writes)
- **Background hosted services:**
  - `EventsPublisher` — Polls outbox and sends events to the broker
  - `EventsProcessor` — Polls inbox and dispatches events to MediatR handlers
  - `ProcessedEventsCleaner` / `ProducedEventsCleaner` — Housekeeping
- **Distributed locking** via `DistributedLock.Postgres` to coordinate across instances

### Transport Layer

Each transport implements a consumer and a transport service:
- **RabbitMQ**: `RabbitConsumer` uses Single Active Consumer (SAC) for ordered delivery per queue; auto-creates queues and exchange bindings
- **Kafka**: `KafkaConsumer` relies on partition-based ordering; topics must be **pre-created manually**
- **Npgsql**: Uses PostgreSQL LISTEN/NOTIFY as a lightweight broker

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
