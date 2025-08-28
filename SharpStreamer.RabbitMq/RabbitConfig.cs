using System.ComponentModel.DataAnnotations;

namespace SharpStreamer.RabbitMq;

public class RabbitConfig
{
   [Required]
   public string UserName { get; set; }

   [Required]
   public string Password { get; set; }

   [Required]
   public string VirtualHost { get; set; }

   [Required]
   public List<RabbitAddr> Addresses { get; set; }

   [Required]
   public List<RabbitTopic> ConsumeTopics { get; set; }
}

public class RabbitAddr
{
   [Required]
   public string Ip { get; set; }

   [Required]
   public int Port { get; set; }
}

public class RabbitTopic
{
   [Required]
   public string Name { get; set; }

   [Required]
   public int PartitionsCount { get; set; }

   [Required]
   public int RetentionTimeInMinutes { get; set; }
}