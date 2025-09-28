namespace DotNetCore.SharpStreamer.Services.Abstractions;

public interface IIdGenerator
{
    Guid GenerateId();
}