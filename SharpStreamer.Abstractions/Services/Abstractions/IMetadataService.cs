using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions.Services.Models;

namespace SharpStreamer.Abstractions.Services.Abstractions;

public interface IMetadataService
{
    /// <summary>
    /// Key : {eventName}:{consumerGroup}, Value: Consumer metadata.
    /// It should be accessed from only Cache class and ca be marked as private.
    /// </summary>
    IReadOnlyDictionary<string, ConsumerMetadata> ConsumersMetadata { get; }

    void AddServicesAndCache(IServiceCollection services, Assembly[] assemblies);

    List<string> GetAllConsumerGroups();
}