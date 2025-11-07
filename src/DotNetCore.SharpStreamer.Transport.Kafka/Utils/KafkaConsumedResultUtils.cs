using System.Text;
using Confluent.Kafka;

namespace DotNetCore.SharpStreamer.Transport.Kafka.Utils;

public static class KafkaConsumedResultUtils
{
    public static string GetHeaderValue(this ConsumeResult<string,string> cr, string key)
    {
        if (cr.Message.Headers.TryGetLastBytes(key, out byte[] value))
        {
            return Encoding.UTF8.GetString(value);
        };

        throw new Exception("Invalid header value");
    }
}