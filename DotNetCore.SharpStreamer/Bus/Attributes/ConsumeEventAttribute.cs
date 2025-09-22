namespace DotNetCore.SharpStreamer.Bus.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConsumeEventAttribute(string eventName) : Attribute
{
    public string EventName => eventName;
}