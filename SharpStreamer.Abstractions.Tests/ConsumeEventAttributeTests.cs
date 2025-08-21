using FluentAssertions;
using SharpStreamer.Abstractions.Attributes;

namespace SharpStreamer.Abstractions.Tests;

public class ConsumeEventAttributeTests
{
    [Theory]
    [InlineData("name", "group", true, null)]
    [InlineData("name", ":group", false, "consumerGroupName must not contain ':' and consumerGroupName must not be null!")]
    [InlineData("name", null, false, "consumerGroupName must not contain ':' and consumerGroupName must not be null!")]
    [InlineData("name:", "group", false, "eventName must not contain ':' and eventName must not be null!")]
    [InlineData(null, "group", false, "eventName must not contain ':' and eventName must not be null!")]
    public void Constructor_WhenIncorrectArgs_ShouldThrowArgumentException
        (string eventName, string consumerGroupName, bool shouldSucceed, string expectedExceptionMessage)
    {
        // Act && Assert
        if (shouldSucceed)
        {
            ConsumeEventAttribute attribute = new ConsumeEventAttribute(eventName, consumerGroupName);   
        }
        else
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new ConsumeEventAttribute(eventName, consumerGroupName));

            ex.Should().NotBeNull();
            ex.Message.Should().Be(expectedExceptionMessage);
        }
    }
}