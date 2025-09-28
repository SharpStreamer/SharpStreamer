using System.Reflection;
using DotNetCore.SharpStreamer.Bus.Attributes;
using Mediator;

namespace DotNetCore.SharpStreamer.Utils;

public static class TypeExtensions
{
    private static readonly Type RequestType = typeof(IRequest);

    public static bool IsLegitConsumableEvent(this Type type)
    {
        return type.IsAssignableTo(RequestType) &&
               type.GetCustomAttribute<ConsumeEventAttribute>() is not null;
    }
}