namespace DotNetCore.SharpStreamer.Bus.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConsumeEventAttribute(string eventName, bool checkPredecessor = true) : Attribute
{
    public string EventName => eventName;

    public bool CheckPredecessor => checkPredecessor;
}