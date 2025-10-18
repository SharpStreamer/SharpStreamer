using System.ComponentModel.DataAnnotations;

namespace DotNetCore.SharpStreamer.Options;

#nullable disable

public class SharpStreamerOptions
{
    [Required]
    public string ConsumerGroup { get; set; }

    [Required]
    public int ProcessorThreadCount { get; set; }

    /// <summary>
    /// This consumer thread count will only be used in transports where we are registering consumers
    /// ex: kafka, RabbitMq ...
    /// ex: Postgres transport doesn't need Consumer thread count because there isn't consumer registered.
    /// </summary>
    public int ConsumerThreadCount { get; set; }

    [Required]
    public int ProcessingBatchSize { get; set; }
}