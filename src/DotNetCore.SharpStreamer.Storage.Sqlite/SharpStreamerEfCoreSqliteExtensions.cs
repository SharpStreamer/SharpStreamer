using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using DotNetCore.SharpStreamer.Storage.Sqlite.Abstractions;
using DotNetCore.SharpStreamer.Storage.Sqlite.Jobs;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: InternalsVisibleTo("Storage.Sqlite.Tests")]

namespace DotNetCore.SharpStreamer.Storage.Sqlite;

public static class SharpStreamerEfCoreSqliteExtensions
{
    private static readonly string EventStatusComment = BuildEventStatusComment();

    private static string BuildEventStatusComment()
    {
        EventStatus[] values = Enum.GetValues<EventStatus>();
        return string.Join(',', values.Select(status => $"{status} = {(int)status}"));
    }

    public static IServiceCollection AddSharpStreamerStorageSqlite<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IStreamerBus, StreamerBusSqlite<TDbContext>>();
        services.TryAddSingleton<IDistributedLockProvider, SharpStreamerDistributedLockProviderSqlite<TDbContext>>();
        services.AddScoped<IEventsRepository, EventsRepository<TDbContext>>();
        services.AddHostedService<EventsProcessor>();
        services.AddHostedService<EventsPublisher>();
        services.AddHostedService<ProcessedEventsCleaner>();
        services.AddHostedService<ProducedEventsCleaner>();
        services.AddSingleton<IMigrationService, MigrationService<TDbContext>>();
        services.AddScoped<IEventsProcessor, EventsProcessorService>();
        services.AddScoped<IEventProcessor, EventProcessorService>();
        return services;
    }

    internal static ModelBuilder ConfigureSharpStreamerSqlite(this ModelBuilder modelBuilder)
    {
        ConfigurePublishedEvents(modelBuilder);
        ConfigureReceivedEvents(modelBuilder);
        return modelBuilder;
    }

    private static void ConfigureReceivedEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceivedEvent>(entity =>
        {
            entity.ToTable("received_events");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Group)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasComment(EventStatusComment);

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SentAt)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000)
                .HasDefaultValue(null)
                .IsRequired(false);

            entity.Property(e => e.UpdateTimestamp)
                .HasDefaultValue(null)
                .IsRequired(false);

            entity.Property(e => e.Partition)
                .HasMaxLength(500)
                .HasDefaultValue(null)
                .IsRequired(false);

            entity.Property(e => e.EventKey)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(e => new { e.EventKey, e.Status, e.Timestamp })
                .HasDatabaseName("IX_EventKey_Status")
                .HasFilter("\"Status\" = 0 OR \"Status\" = 3 OR \"Status\" = 1");

            entity.HasIndex(e => new { e.Status, e.UpdateTimestamp, e.RetryCount })
                .HasDatabaseName("IX_Events_For_Processing");

            entity.HasIndex(e => new { e.Timestamp })
                .HasDatabaseName("IX_ReceivedEvents_Timestamp");
        });
    }

    private static void ConfigurePublishedEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PublishedEvent>(entity =>
        {
            entity.ToTable("published_events");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasComment(EventStatusComment);

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SentAt)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.EventKey)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(e => new { e.Status, e.SentAt })
                .HasDatabaseName("IX_Events_For_Publishing");

            entity.HasIndex(e => new { e.Timestamp })
                .HasDatabaseName("IX_PublishedEvents_Timestamp");
        });
    }
}
