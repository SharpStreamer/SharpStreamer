namespace DotNetCore.SharpStreamer.Services.Models;

public class ConsumerMetadata
{
    public required Type EventType { get; set; }

    public required bool NeedsToBeCheckedPredecessor { get; set; }
}