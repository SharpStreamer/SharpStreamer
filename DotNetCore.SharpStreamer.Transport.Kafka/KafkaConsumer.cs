using Confluent.Kafka;
using DotNetCore.SharpStreamer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

public class KafkaConsumer(
    IOptions<SharpStreamerOptions> sharpStreamerOptions,
    IOptions<KafkaOptions> kafkaOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (sharpStreamerOptions.Value.ConsumerThreadCount == 0)
        {
            throw new ArgumentException("When dealing with kafka, consumer thread count must be greater than zero.");
        }

        List<Task> consumerTasks = new();
        for (int i = 0; i < sharpStreamerOptions.Value.ConsumerThreadCount; i++)
        {
            consumerTasks.Add(Task.Run(async () => await RunConsumer(), stoppingToken));
        }
        await Task.WhenAll(consumerTasks);
    }

    private async Task RunConsumer()
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