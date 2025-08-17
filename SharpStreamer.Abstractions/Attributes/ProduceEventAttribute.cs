namespace SharpStreamer.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ProduceEventAttribute(string EventName, string TopicName) : Attribute;