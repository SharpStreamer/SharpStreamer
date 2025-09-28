using System.ComponentModel.DataAnnotations;

namespace DotNetCore.SharpStreamer.Options;

#nullable disable

public class SharpStreamerOptions<TTopicOptions>
{
    [Required]
    public string BaseConsumerGroupName { get; set; }

    [Required]
    public int ProcessorThreadCount { get; set; }

    [Required]
    public List<TTopicOptions> Topics { get; set; }
}