using System.ComponentModel.DataAnnotations;

namespace DotNetCore.SharpStreamer.Transport.Kafka;

#nullable disable

public class KafkaOptions
{
    /// <summary>
    /// Comma seperated server:port tuples,
    /// ex: localhost:9092,localhost:9093,localhost:9094
    /// </summary>
    [Required]
    public string Servers { get; set; }

    [Required]
    public List<string> TopicsToBeConsumed { get; set; }

    /// <summary>
    /// Commits kafka, after consuming 'CommitBatchSize' amount of messages.
    /// </summary>
    [Required]
    public int CommitBatchSize { get; set; }

    /// <summary>
    /// Determines connections count, how many connection will be created for producing events
    /// </summary>
    [Required]
    public int ProducersCount { get; set; }
}