using System.ComponentModel.DataAnnotations;
using RabbitMQ.Client;

#nullable disable

namespace DotNetCore.SharpStreamer.Transport.RabbitMq;

public class RabbitOptions
{
    [Required]
    public List<string> Topics { get; set; }

    [Required]
    public ConnectionFactory ConnectionSettings { get; set; } 

    /// <summary>
    /// Performs a commit to RabbitMQ once CommitBatchSize messages have been consumed.
    /// </summary>
    [Required]
    public int CommitBatchSize { get; set; }

    /// <summary>
    /// Commits messages to RabbitMQ if no commit has occurred within CommitTimespanSeconds.
    /// If CommitBatchSize messages have been consumed before that time, a commit is triggered immediately.
    /// </summary>
    [Required]
    public int CommitTimespanSeconds { get; set; }
}