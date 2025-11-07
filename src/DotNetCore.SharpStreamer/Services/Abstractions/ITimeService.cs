namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface ITimeService
{
    DateTimeOffset GetUtcNow();

    Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken = default);
}