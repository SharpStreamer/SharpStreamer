using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public class RabbitConnectionProvider(
    IOptions<RabbitOptions> rabbitOptions,
    ILogger<RabbitConnectionProvider> logger)
    : IDisposable, IAsyncDisposable
{
    private IConnection? _connection = null;
    private readonly SemaphoreSlim _connectionLock = new(1);
    
    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }


        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is null)
            {
                SetupConnectionFactoryAutomaticParts(rabbitOptions.Value.ConnectionSettings);
                _connection = await rabbitOptions.Value.ConnectionSettings.CreateConnectionAsync();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating RabbitMQ connection");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }

        return _connection;
    }

    private static void SetupConnectionFactoryAutomaticParts(ConnectionFactory valueConnectionSettings)
    {
        valueConnectionSettings.AutomaticRecoveryEnabled = true;
        valueConnectionSettings.TopologyRecoveryEnabled = true; // Re-declares queues, exchanges, and bindings
    
        // How long to wait before trying to reconnect.
        valueConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
    
        // Heartbeats help detect a "zombie" connection.
        valueConnectionSettings.RequestedHeartbeat = TimeSpan.FromSeconds(30);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}