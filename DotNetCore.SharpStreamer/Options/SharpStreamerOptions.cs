using System.ComponentModel.DataAnnotations;

namespace DotNetCore.SharpStreamer.Options;

#nullable disable

public class SharpStreamerOptions
{
    [Required]
    public string ConsumerGroup { get; set; }

    [Required]
    public int ProcessorThreadCount { get; set; }

    [Required]
    public int ProcessingBatchSize { get; set; }
}