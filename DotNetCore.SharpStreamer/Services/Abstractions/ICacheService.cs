using System.Runtime.CompilerServices;
using DotNetCore.SharpStreamer.Services.Models;

[assembly: InternalsVisibleTo("DotNetCore.SharpStreamer.Storage.Npgsql")]

namespace DotNetCore.SharpStreamer.Services.Abstractions;

internal interface ICacheService
{
    bool TryCacheConsumer(Type type);

    /// <summary>
    /// Returns metadata of consumer which consumes event with name of 'eventName'
    /// </summary>
    ConsumerMetadata? GetConsumerMetadata(string eventName);

    PublishableEventMetadata GetOrCreatePublishableEventMetadata<T>();
}