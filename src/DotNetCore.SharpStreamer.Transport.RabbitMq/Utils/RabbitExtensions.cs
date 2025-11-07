using System.Text;
using RabbitMQ.Client.Events;

namespace DotNetCore.SharpStreamer.Transport.RabbitMq.Utils;

public static class RabbitExtensions
{
    public static string GetHeaderValue(this BasicDeliverEventArgs ea, string key)
    {
        if (ea.BasicProperties.Headers is not null &&
            ea.BasicProperties.Headers.TryGetValue(key, out object? value))
        {
            if (value is null)
            {
                throw new ArgumentException($"value of header in {key} is null.");
            }
            byte[] arr = (byte[])value;
            return Encoding.UTF8.GetString(arr);
        }

        throw new ArgumentException($"Header not found, {key}");
    }
}