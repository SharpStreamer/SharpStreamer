using System.Text.Json;

namespace DotNetCore.SharpStreamer.Utils;

public class JsonExtensions
{
    public static readonly JsonSerializerOptions SharpStreamerJsonOptions = new JsonSerializerOptions()
    {
    };
}