using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.EfCore.Npgsql.Jobs;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Enums;
using DotNetCore.SharpStreamer.Repositories.Abstractions;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public static class SharpStreamerEfCoreNpgsqlExtensions
{
    private static readonly string EventStatusComment = BuildEventStatusComment();

    private static string BuildEventStatusComment()
    {
        EventStatus[] values = Enum.GetValues<EventStatus>();
        return string.Join(',', values.Select(status => $"{status} = {(int)status}"));
    }

    public static IServiceCollection AddSharpStreamerNpgsql<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IStreamerBus, StreamerBusNpgsql<TDbContext>>();
        services.AddScoped<IConsumerService, ConsumerNpgsqlService<TDbContext>>();
        services.TryAddSingleton<IDistributedLockProvider, SharpStreamerDistributedLockProviderNpgsql<TDbContext>>();
        services.AddScoped<IEventsRepository, EventsRepository<TDbContext>>();
        services.AddHostedService<EventsProcessor>();
        return services;
    }

    public static ModelBuilder ConfigureSharpStreamerNpgsql(this ModelBuilder modelBuilder)
    {
        ConfigurePublishedEvents(modelBuilder);
        ConfigureReceivedEvents(modelBuilder);
        return modelBuilder;
    }

    private static void ConfigureReceivedEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceivedEvent>(entity =>
        {
            entity.ToTable("received_events", "sharp_streamer");

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
                .HasColumnType("json");

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SentAt)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(1000)
                .HasDefaultValue(null)
                .IsRequired(false);

            entity.Property(e => e.UpdateTimestamp)
                .HasColumnType("timestamptz")
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
                .HasFilter("\"Status\" = 0 or \"Status\" = 3 or \"Status\" = 1")
                .HasDatabaseName("IX_EventKey_Status");

            entity.HasIndex(e => new { e.Status, e.UpdateTimestamp, e.RetryCount })
                .HasDatabaseName("IX_Events_For_Processing");
        });
    }

    private static void ConfigurePublishedEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PublishedEvent>(entity =>
        {
            entity.ToTable("published_events", "sharp_streamer");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasComment(EventStatusComment);

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnType("json");

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SentAt)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamptz")
                .IsRequired();

            entity.Property(e => e.EventKey)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .HasColumnType("timestamptz")
                .IsRequired();
        });
    }
}