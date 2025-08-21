namespace SharpStreamer.Abstractions.Services.Models;

public class ConsumerMetadata
{
    public required Type ConsumerType { get; set; }

    public required Type EventType { get; set; }
}