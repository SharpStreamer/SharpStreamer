using DotNetCore.SharpStreamer.Entities.Abstractions;
using DotNetCore.SharpStreamer.Enums;

#nullable disable
namespace DotNetCore.SharpStreamer.Entities;

public class ReceivedEvent : Event<Guid>
{
    public string Group { get; set; }
}