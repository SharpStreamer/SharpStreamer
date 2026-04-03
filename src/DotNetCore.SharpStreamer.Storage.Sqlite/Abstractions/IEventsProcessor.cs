namespace DotNetCore.SharpStreamer.Storage.Sqlite.Abstractions;

internal interface IEventsProcessor
{
    Task ProcessEvents();
}
