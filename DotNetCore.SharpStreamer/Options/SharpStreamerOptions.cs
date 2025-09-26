using System.ComponentModel.DataAnnotations;

namespace DotNetCore.SharpStreamer.Options;

#nullable disable

public class SharpStreamerOptions<TTopicMetadata>
{
    [Required]
    public string BaseConsumerGroupName { get; set; }

    [Required]
    public List<TTopicMetadata> Topics { get; set; }
}