using DotNetCore.SharpStreamer.Entities;

namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface ITransportService
{
    Task Publish(PublishedEvent publishedEvent);
}