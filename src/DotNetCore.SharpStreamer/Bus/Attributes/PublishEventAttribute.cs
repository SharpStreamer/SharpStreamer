namespace DotNetCore.SharpStreamer.Bus.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PublishEventAttribute(string eventName, string topicName) : Attribute
{
    public string EventName => eventName;
    public string TopicName => topicName;
}