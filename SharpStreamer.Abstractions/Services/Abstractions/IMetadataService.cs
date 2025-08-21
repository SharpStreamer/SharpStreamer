using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions.Services.Models;

namespace SharpStreamer.Abstractions.Services.Abstractions;

public interface IMetadataService
{
    IReadOnlyDictionary<string, ConsumerMetadata> ConsumersMetadata { get; }

    void AddServicesAndCache(IServiceCollection services, Assembly[] assemblies);
}