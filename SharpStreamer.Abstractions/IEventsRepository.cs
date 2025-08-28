namespace SharpStreamer.Abstractions;

public interface IEventsRepository
{
    Task CreateIfNotExists(EventEntity eventEntity);
}