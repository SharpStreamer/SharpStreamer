using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SharpStreamer.Abstractions.Services;
using SharpStreamer.Abstractions.Services.Abstractions;
using SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses;
using SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Customers;
using SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Orders;
using SharpStreamer.Abstractions.Tests.AddSharpStreamerRabbitMqDependencyInjectionClasses.Users;

namespace SharpStreamer.Abstractions.Tests;

public class MetadataServiceTests
{
    private readonly IMetadataService _metadataService;

    public MetadataServiceTests()
    {
        _metadataService = new MetadataService();
    }

    [Fact]
    public void AddServicesAndCache_WhenOneClassIsChildOfMultipleConsumer_AddsConsumersCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderSubmitted>) && x.ImplementationType == typeof(OrdersHandler));
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderShipped>) && x.ImplementationType == typeof(OrdersHandler));
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<OrderCreated>) && x.ImplementationType == typeof(OrdersHandler));
    }

    [Fact]
    public void AddServicesAndCache_WhenOneClassIsChildOfOneConsumer_AddsConsumersCorrectly()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        services.Should().Contain(x => x.ServiceType == typeof(IConsumer<CustomerCreated>) && x.ImplementationType == typeof(CustomerCreatedHandler));
    }

    [Fact]
    public void AddServicesAndCache_WhenClassIsNotConsumer_ShouldNotAddConsumer()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        services.Should().NotContain(x => x.ImplementationType == typeof(NotConsumer));
    }

    [Fact]
    public void AddServicesAndCache_WhenClassIsConsumerButEventDoesNotHaveConsumeEventAttribute_ShouldNotAddConsumer()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        services.Should().NotContain(x => x.ServiceType == typeof(IConsumer<UserCreated>));
    }

    [Fact]
    public void AddServicesAndCache_WhenConsumersAddedInDi_TypesShouldBeCached()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        _metadataService.ConsumersMetadata.Should().ContainKeys(
            "order_created:tests_consumer_group_1",
            "order_shipped:tests_consumer_group_1",
            "order_submitted:tests_consumer_group_1",
            "customer_created:tests_consumer_group_2");
        _metadataService.ConsumersMetadata["order_created:tests_consumer_group_1"].ConsumerType.Should().Be(typeof(IConsumer<OrderCreated>));
        _metadataService.ConsumersMetadata["order_created:tests_consumer_group_1"].EventType.Should().Be(typeof(OrderCreated));
        _metadataService.ConsumersMetadata["order_shipped:tests_consumer_group_1"].ConsumerType.Should().Be(typeof(IConsumer<OrderShipped>));
        _metadataService.ConsumersMetadata["order_shipped:tests_consumer_group_1"].EventType.Should().Be(typeof(OrderShipped));
        _metadataService.ConsumersMetadata["order_submitted:tests_consumer_group_1"].ConsumerType.Should().Be(typeof(IConsumer<OrderSubmitted>));
        _metadataService.ConsumersMetadata["order_submitted:tests_consumer_group_1"].EventType.Should().Be(typeof(OrderSubmitted));
        _metadataService.ConsumersMetadata["customer_created:tests_consumer_group_2"].ConsumerType.Should().Be(typeof(IConsumer<CustomerCreated>));
        _metadataService.ConsumersMetadata["customer_created:tests_consumer_group_2"].EventType.Should().Be(typeof(CustomerCreated));
    }

    [Fact]
    public void GetAllConsumerGroups_WhenConsumersExists_ReturnsDistinctConsumerGroups()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        // Act
        _metadataService.AddServicesAndCache(services, [ Assembly.GetExecutingAssembly() ]);

        // Assert
        List<string> consumerGroups = _metadataService.GetAllConsumerGroups();
        consumerGroups.Should().BeEquivalentTo(["tests_consumer_group_1", "tests_consumer_group_2"]);
    }
}