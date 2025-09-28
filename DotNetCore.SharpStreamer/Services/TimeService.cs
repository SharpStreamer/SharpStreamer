using DotNetCore.SharpStreamer.Services.Abstractions;

namespace DotNetCore.SharpStreamer.Services;

public class TimeService(TimeProvider timeProvider) : ITimeService
{
    public DateTimeOffset GetUtcNow()
    {
        return timeProvider.GetUtcNow();
    }

    public async Task Delay(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        await Task.Delay(timeSpan, cancellationToken);
    }
}