namespace SharpStreamer.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ConsumeEventAttribute : Attribute
{
    public string EventName { get; init; }
    public string ConsumerGroupName { get; init; }

    public ConsumeEventAttribute(string? eventName, string? consumerGroupName)
    {
        if (eventName is null || eventName.Contains(':'))
        {
            throw new ArgumentException("eventName must not contain ':' and eventName must not be null!");
        }
        EventName = eventName;

        if (consumerGroupName is null || consumerGroupName.Contains(':'))
        {
            throw new ArgumentException("consumerGroupName must not contain ':' and consumerGroupName must not be null!");
        }
        ConsumerGroupName = consumerGroupName;
    }
}