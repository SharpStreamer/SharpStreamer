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
}