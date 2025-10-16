using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public class KafkaConsumer : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "my-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe("test_topic");

        Console.WriteLine("Consuming messages from 'test_topic'...");
        
        Console.WriteLine("Consuming messages from 'test_topic'...");

        try
        {
            int counter = 1;
            while (true)
            {
                var cr = consumer.Consume(CancellationToken.None);
                Console.WriteLine($"Received message: {cr.Message.Value}");
                counter++;
                if (counter % 10 == 0)
                {
                    consumer.Commit(cr);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Clean shutdown
        }
        finally
        {
            consumer.Close();
        }
    }
}