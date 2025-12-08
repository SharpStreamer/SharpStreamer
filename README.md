* KAFKA
    * In case of kafka, topics defined in appsettings, must be pre-created in kafka. library doesn't handle topic creation on it's own
      and it needs to be predefined.
* Delayed events WARNING:
      
  The library provides built-in support for delayed event publishing, allowing you to schedule an event to be published after a specific period of time.

  However, delayed events always use a random GUID as their event key, rather than a user-defined or deterministic one.

  Why random event keys?

  In normal (immediate) events, you might want to use a deterministic event key to ensure ordering — for example, so that events with the same key are processed sequentially.
  But delayed events behave differently:

  Because they are scheduled for future publication, using a deterministic key could unintentionally block or delay other unrelated events that share the same key.

  This can easily happen without the developer realizing it, especially when delayed events interact with normal ones in the same event stream.

  To avoid this subtle and hard-to-debug problem, delayed events are always assigned a unique, random key.

  What this means for you

  Delayed events do not guarantee ordering relative to other events.

  You should treat delayed events as independent — they will be published later, but not necessarily in sequence with other events.

  If you require ordered processing, you should use immediate events with deterministic keys instead.

  This design helps keep event processing predictable and prevents unintended blocking or ordering issues in your system.
* Architectural Documentation: Eventing Library
This document outlines the architecture, core components, and operational mechanics of the custom eventing library, designed to facilitate reliable, ordered, and transactional event communication between microservices.

1. Core Principles
   * The library is built upon the Transactional Outbox/Inbox Pattern to guarantee at-least-once delivery and ensure that event publishing and consumption are transactional with the microservice's local database operations.

   * Reliability: Achieved via persistence in dedicated Published and Received tables.

   * Ordered Processing: Ensured by the eventKey mechanism, which guarantees sequential processing for all events sharing the same key.

   * Decoupling: Supports multiple message brokers (e.g., Kafka, RabbitMQ) while providing a consistent API for the microservices.
2. Architectural Components and Flow
The architecture is centered around two main flows: Event Publishing (Outbox) and Event Consumption (Inbox).

2.1. Event Publishing Flow (Outbox Pattern)
   The library implements the Outbox Pattern to ensure that an event is only considered successfully published if the microservice's business logic transaction commits successfully.

   Event Insertion: When a microservice intends to publish an event, the event data, including the mandatory eventKey, is atomically inserted into the local database table, Published (the Outbox). This insertion occurs within the same database transaction as the business operation that triggered the event.
   
   Outbox Polling/Relaying: A separate, background process (often called a Relay or Outbox Poller) monitors the Published table for new, unprocessed events.
   
   Broker Publishing: The Relay process fetches the event and publishes it to the configured Message Broker (e.g., Kafka, RabbitMQ).
   
   Status Update: Upon successful delivery to the broker, the Relay process updates the event's status in the Published table (e.g., marks it as Processed or Sent).
2.2. Event Consumption Flow (Inbox Pattern)
   The library implements the Inbox Pattern to handle incoming events, ensuring events are not lost and can be processed idempotently.
   
   Broker Reception: The microservice receives an event from the Message Broker.
   
   Inbox Insertion: Before executing any business logic, the incoming event (with its unique identifier and eventKey) is inserted into the local database table, Received (the Inbox).
   
   Processing/Transaction: The business logic is executed. This step, including the Inbox insertion, must be handled idempotently to tolerate retries.
   
   Status Update: Upon successful processing, the event's status in the Received table is updated (e.g., marked as Completed).
3. Key Feature Deep Dive: Event Ordering (eventKey)
   The eventKey is the critical mechanism for guaranteeing sequential processing of related events while allowing for parallel processing of unrelated events.
   
   Ordering Guarantee
   Events with the same eventKey are guaranteed to be delivered and processed by the consumer in the exact order they were published.
   
   Events with different eventKeys are eligible for parallel processing.
   Kafka - The eventKey is used as the Kafka message key - Kafka guarantees that all messages with the same key will be routed to the same partition. A single consumer process/thread must read from that partition to maintain order.
   RabbitMQ - Used to ensure sequential consumption from a shared queue - Single Active Consumer (SAC): The library utilizes RabbitMQ's SAC feature (or similar exclusive consumption logic). Only one application instance will actively consume from the common queue bound to the required exchanges, thereby preserving the eventKey ordering guarantee at the consumer group level.
4. Consumer Group Management
   Microservice as a Consumer Group
   The design philosophy dictates that each microservice instance acts as a single, distinct Consumer Group.
   
   Constraint: The library forbids the creation or management of multiple consumer groups within a single microservice application instance.
   
   Context: This aligns with the standard Microservices Architecture principle where a consumer group is conceptually tied to a specific application or bounded context.
* RabbitMQ Consumer Setup
   For RabbitMQ, the library handles setup as follows:
   
   Queue Creation: On startup, the microservice application instance creates a new, durable queue.
   
   Exchange Binding: This new queue is automatically bound to all necessary Exchanges (Topics) defined in the service's configuration.
   
   Consumption Strategy: To enforce the eventKey ordering guarantee, the library initiates consumption using the Single Active Consumer (SAC) feature on this queue. This ensures that even if multiple instances of the microservice are running, only one will be actively pulling messages at any given time, preventing out-of-order delivery due to competing consumers.
5. Architectural Advantages
   Transactional Integrity: Outbox pattern guarantees the event is never lost if the originating database transaction succeeds.
   
   Idempotency and Reliability: Inbox pattern facilitates checking for already-processed events, ensuring idempotency and allowing for safe retries.
   
   Controlled Ordering: The eventKey and broker-specific strategies (like SAC in RabbitMQ and partition keys in Kafka) eliminate race conditions for related events.

   SharpStreamer is deeply integrated into EntityFramework, so connection and transaction management will be totally same as you entity framework will manage it. IStreamerBus is Scoped service like DbContext, so it shares same scope as database context and looks at same transactions and connections as your specified DbContext.

* Configuration.
   Currently most stable version is RabbitMq's version, because I use this version in my internal projects, so I am actively contributing it. Other implementations are experimental. You can see configuration example in samples/DotNetCore.SharpStreamer.RabbitMq.Npgsql project. At high-level configuration looks like this:
   ```
   builder.Services.AddDbContext<RabbitNpgDbContext>(options =>
   {
       options.UseNpgsql("Pooling=True;Maximum Pool Size=100;Minimum Pool Size=1;Connection Idle Lifetime=60;Host=localhost;Port=5435;Database=rabbit_npg_sample;Username=postgres;Password=postgres");
   });
   
   builder.Services.AddMediatR(options =>
   {
       options.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
       options.Lifetime = ServiceLifetime.Transient;
   });
   
   builder.Services
       .AddSharpStreamer("SharpStreamerSettings")
       .AddSharpStreamerStorageNpgsql<RabbitNpgDbContext>()
       .AddSharpStreamerTransportRabbitMq();
   ```
   Project uses MediatR V12 because it was opensource project under MIT licence. Also same version is forked under this organization.
* Package publishing instructions:
    * Navigate into class library project where you want to publish package
    * Then pack this project in release mode:
      *     dotnet pack --configuration Release
    * Then publish this package into nuget provider server like this:
      *     dotnet nuget push .\bin\Release\DotNetCore.SharpStreamer.1.0.0.nupkg -s https://api.nuget.org/v3/index.json -k {Your_API_Key}
        * Verbose example:
          *     dotnet nuget push ./bin/Release/YourPackageName.1.0.0.nupkg \
                --source https://api.nuget.org/v3/index.json \
                --api-key {YOUR_API_KEY}
    * This will publish this package into nuget server provided in this script
    * Change versions in csproj files.
