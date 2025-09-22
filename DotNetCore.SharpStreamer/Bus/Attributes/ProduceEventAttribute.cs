namespace DotNetCore.SharpStreamer.Bus.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ProduceEventAttribute(string eventName, string groupName) : Attribute
{
    public string EventName => eventName;
    public string GroupName => groupName;
}