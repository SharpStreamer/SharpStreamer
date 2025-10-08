using DotNetCore.SharpStreamer.Entities.Abstractions;

#nullable disable
namespace DotNetCore.SharpStreamer.Entities;

public class ReceivedEvent : Event<Guid>
{
    public string Group { get; set; }

    public string ErrorMessage { get; set; }

    public DateTimeOffset? UpdateTimestamp { get; set; }
}