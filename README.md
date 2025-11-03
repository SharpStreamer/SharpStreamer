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
* How to configure:
    * In DbContext's OnModelCreating method add following line and run AddMigration to add necessary tables in database
      *     protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.ConfigureSharpStreamerNpgsql();
                base.OnModelCreating(modelBuilder);
            }
    * Configure DI like this:
      *     builder.Services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
                options.Lifetime = ServiceLifetime.Transient;
            });
            builder.Services
                .AddSharpStreamer("YourSettingsName", Assembly.GetExecutingAssembly())
                .AddSharpStreamerStorageNpgsql<YourDbContext>()
                .AddSharpStreamerTransportNpgsql();
    * After this configuration you can freely use IStreamerBus. IStreamerBus is scoped service and uses YourDbContext, 
      so if you start Transaction in the scope of
      YourDbContext, IStreamerBus will use this transaction too.
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
