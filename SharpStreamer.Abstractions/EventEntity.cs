using System.Runtime.CompilerServices;
using System.Text.Json;
[assembly: InternalsVisibleTo("SharpStreamer.EntityFrameworkCore.Npgsql")]

namespace SharpStreamer.Abstractions;

public class EventEntity
{
    public Guid Id { get; set; }

    public string EventBody { get; set; }

    public string EventHeaders { get; set; }

    public string EventKey { get; set; }

    public int TryCount { get; set; }

    public EventFlags Flags { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime Timestamp { get; set; }

    public string ConsumerGroup { get; set; }

    public EventEntity WithHeaders(Dictionary<string, string> headers)
    {
        EventHeaders = JsonSerializer.Serialize(headers, Constants.SerializerOptions);
        return this;
    }

    public Dictionary<string, string> Headers =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(EventHeaders, Constants.SerializerOptions)
        ?? throw new Exception("SharpStreamer => Headers not found for EventEntity");
}