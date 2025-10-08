﻿using System.Reflection;
using DotNetCore.SharpStreamer.Bus.Attributes;
using MediatR;

namespace DotNetCore.SharpStreamer.Utils;

public static class TypeExtensions
{
    private static readonly Type RequestType = typeof(IRequest);

    public static bool IsLegitConsumableEvent(this Type type)
    {
        return type.IsAssignableTo(RequestType) &&
               type.GetCustomAttribute<ConsumeEventAttribute>() is not null;
    }

    public static bool IsLegitPublishableEvent(this Type type)
    {
        return type.GetCustomAttribute<PublishEventAttribute>() is not null;
    }
}