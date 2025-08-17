using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions;
using SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses;
using SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Customers;
using SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;
using SharpStreamer.RabbitMq.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Users;

namespace SharpStreamer.RabbitMq.Tests;

public class ExtensionsTests
{
    [Fact]
    public void AddSharpStreamerRabbitMq_WhenOneClassIsChildOfMultipleConsumer_AddsConsumersCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly());

        // Assert
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderSubmitted>) && x.ImplementationType == typeof(OrdersHandler));
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderShipped>) && x.ImplementationType == typeof(OrdersHandler));
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderCreated>) && x.ImplementationType == typeof(OrdersHandler));
    }

    [Fact]
    public void AddSharpStreamerRabbitMq_WhenOneClassIsChildOfOneConsumer_AddsConsumersCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly());

        // Assert
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<CustomerCreated>) && x.ImplementationType == typeof(CustomerCreatedHandler));
    }

    [Fact]
    public void AddSharpStreamerRabbitMq_WhenClassIsNotConsumer_ShouldNotAddConsumer()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly());

        // Assert
        services.Should().NotContain(x => x.ImplementationType == typeof(NotConsumer));
    }

    [Fact]
    public void AddSharpStreamerRabbitMq_WhenClassIsConsumerButEventDoesNotHaveConsumeEventAttribute_ShouldNotAddConsumer()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly());

        // Assert
        services.Should().NotContain(x => x.ServiceType == typeof(IConsumer<UserCreated>));
    }

    [Fact]
    public void AddSharpStreamerRabbitMq_WhenConsumersAddedInDi_TypesShouldBeCached()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        services.AddSharpStreamerRabbitMq(Assembly.GetExecutingAssembly());

        // Assert
        Cache.ConsumersMetadata.Should().ContainKeys(
            "order_created:tests_consumer_group_1",
            "order_shipped:tests_consumer_group_1",
            "order_submitted:tests_consumer_group_1",
            "customer_created:tests_consumer_group_2");
    }
}