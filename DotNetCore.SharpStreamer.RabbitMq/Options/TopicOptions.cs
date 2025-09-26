using System.ComponentModel.DataAnnotations;

#nullable disable

namespace DotNetCore.SharpStreamer.RabbitMq.Options;

public class TopicOptions
{
    [Required]
    public string Name { get; set; }

    [Required]
    public int PartitionCount { get; set; }

    [Required]
    public int RetentionTimeInMinutes { get; set; }
}