using System.Data;
using Dapper;
using DotNetCore.SharpStreamer.Entities;
using DotNetCore.SharpStreamer.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DotNetCore.SharpStreamer.EfCore.Npgsql;

public class ConsumerNpgsqlService<TDbContext>(TDbContext dbContext) : IConsumerService
    where TDbContext : DbContext
{
    public async Task SaveConsumedEvent(ReceivedEvent receivedEvent)
    {
        const string insertQuery = $@"
                            INSERT INTO sharp_streamer.received_events
                            (
                                ""Id"",
                                ""Topic"",
                                ""Content"",
                                ""RetryCount"",
                                ""SentAt"",
                                ""Timestamp"",
                                ""ExpiresAt"",
                                ""Status"",
                                ""Group""
                            )
                            VALUES
                            (
                                @{nameof(ReceivedEvent.Id)},
                                @{nameof(ReceivedEvent.Topic)},
                                @{nameof(ReceivedEvent.Content)}::json,
                                @{nameof(ReceivedEvent.RetryCount)},
                                @{nameof(ReceivedEvent.SentAt)},
                                @{nameof(ReceivedEvent.Timestamp)},
                                @{nameof(ReceivedEvent.ExpiresAt)},
                                @{nameof(ReceivedEvent.Status)},
                                @{nameof(ReceivedEvent.Group)}
                            );";
        IDbConnection dbConnection = dbContext.Database.GetDbConnection();
        IDbTransaction? dbTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        await dbConnection.ExecuteAsync(sql: insertQuery, param: receivedEvent, transaction: dbTransaction);
    }
}