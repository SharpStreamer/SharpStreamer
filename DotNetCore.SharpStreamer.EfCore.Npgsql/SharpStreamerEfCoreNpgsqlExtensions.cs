using DotNetCore.SharpStreamer.Bus;
using DotNetCore.SharpStreamer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public static class SharpStreamerEfCoreNpgsqlExtensions
{
    public static IServiceCollection AddSharpStreamerNpgsql<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IStreamerBus, StreamerBusNpgsql<TDbContext>>();
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
                .HasConversion<string>()
                .HasMaxLength(100);

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
                .HasConversion<string>()
                .HasMaxLength(100);

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
        });
    }
}