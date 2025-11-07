using DotNetCore.SharpStreamer.Enums;

#nullable disable
namespace DotNetCore.SharpStreamer.Entities.Abstractions;

public abstract class Event<TId>
{
    public TId Id { get; set; }

    public string Topic { get; set; }

    public string Content { get; set; }

    public int RetryCount { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public EventStatus Status { get; set; }

    public string EventKey { get; set; }
}