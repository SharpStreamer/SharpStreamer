using DotNetCore.SharpStreamer.Services.Abstractions;

namespace DotNetCore.SharpStreamer.Services;

public class UlidGenerator : IIdGenerator
{
    public Guid GenerateId()
    {
        return Guid.CreateVersion7();
    }
}