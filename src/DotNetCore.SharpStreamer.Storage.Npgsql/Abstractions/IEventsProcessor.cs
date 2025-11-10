namespace DotNetCore.SharpStreamer.Storage.Npgsql.Abstractions;

internal interface IEventsProcessor
{
    Task ProcessEvents();
}