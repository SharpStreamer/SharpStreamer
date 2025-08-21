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
}

public class RabbitAddr
{
   [Required]
   public string Ip { get; set; }

   [Required]
   public int Port { get; set; }
}