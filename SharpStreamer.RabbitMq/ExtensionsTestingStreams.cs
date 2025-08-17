using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Stream.Client;
using RabbitMQ.Stream.Client.Reliable;

namespace SharpStreamer.RabbitMq;

public static class ExtensionsTestingStreams
{
    public static async Task TestStreamer(ILogger<Producer> loggerProducer, ILogger<StreamSystem> loggerStreamSystem, ILogger<Consumer> consumerLogger)
    {
        var streamSystem = await StreamSystem.Create(
            new StreamSystemConfig()
            {
                UserName = "admin",
                Password = "password",
                Endpoints = new List<EndPoint>() { new IPEndPoint(IPAddress.Loopback, 5552) }
            },loggerStreamSystem);
        const string streamName = "test.stream";
        await streamSystem.CreateStream(
            new StreamSpec(streamName)
            {
                MaxAge = TimeSpan.FromDays(14), // message retention time
            });

        Producer producer = await Producer.Create(
            new ProducerConfig(streamSystem, streamName)
            {
                ConfirmationHandler = TestStreamer, 
            }, loggerProducer);

        ulong existingOffset = await CheckExistingOffset(streamSystem);
        var consumer = await streamSystem.CreateRawConsumer(new RawConsumerConfig(streamName)
        {
            OffsetSpec = new OffsetTypeOffset(existingOffset),
            Reference = "test.group", // This is the consumer group name
            MessageHandler = async (consumer, context, message) =>
            {
                try
                {
                    // Process the message
                    Console.WriteLine(Encoding.ASCII.GetString(message.Data.Contents) + " - "  + context.Offset);
                            
                    // Commit offset after successful processing
                    await consumer.StoreOffset(context.Offset);
                            
                    Console.WriteLine($"✓ Processed and committed offset: {context.Offset}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error processing message at offset {context.Offset}: {ex.Message}");
                    // Don't commit offset on error - message will be reprocessed
                }
            },
            IsSingleActiveConsumer = true,
        }, consumerLogger);
        
        //Console.WriteLine("Starting publishing...");
        // for (var i = 0; i < 100; i++)
        // {
        //     await producer.Send(new Message(Encoding.ASCII.GetBytes($"opanaaa message-{i}")));
        //     await Task.Delay(1000);
        // }
        await producer.Close();

        await Task.Delay(100000);
        await consumer.Close();
    }

    public static async Task TestStreamer(MessagesConfirmation confirmation)
    {
        // here you can handle the confirmation
        switch (confirmation.Status)
        {
            case ConfirmationStatus.Confirmed:
                // all the messages received here are confirmed
                Console.WriteLine("confirmed " + Encoding.ASCII.GetString(confirmation.Messages[0].Data.Contents));
                break;
            case ConfirmationStatus.StreamNotAvailable:
            case ConfirmationStatus.InternalError:
            case ConfirmationStatus.AccessRefused:
            case ConfirmationStatus.PreconditionFailed:
            case ConfirmationStatus.PublisherDoesNotExist:
            case ConfirmationStatus.UndefinedError:
            case ConfirmationStatus.ClientTimeoutError:
                Console.WriteLine($"Message {confirmation.PublishingId} failed with{confirmation.Status}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await Task.Yield();
    }
    
    private static async Task<ulong> CheckExistingOffset(StreamSystem streamSystem)
    {
        try
        {
            ulong existingOffset = await streamSystem.QueryOffset("test.group", "test.stream");
            Console.WriteLine($"📍 Found existing offset: {existingOffset}");
            Console.WriteLine($"📍 Will resume from: {existingOffset + 1}");
            return existingOffset + 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"📍 No existing offset found: {ex.Message}");
            Console.WriteLine($"📍 Will start from beginning for new consumer group");
            return 0;
        }
    }
}