using System.Text.Json.Serialization;
using Enumbrella.Sample;

namespace Enumbrella.Tests;

internal enum Priority
{
    High = 10,
    Low = 1,
    Medium = 5,
}

internal enum Temperature
{
    Warm = 5,
    Freezing = -10,
    Mild = 0,
    Cold = -1,
}

internal enum Level : byte
{
    Low = 1,
    High = 255,
}

internal enum Distance : long
{
    Near = 1,
    Far = 10_000_000_000,
}

internal sealed record PriorityResponse(Priority Priority);

internal sealed record TemperatureResponse(Temperature Temperature);

internal sealed record LevelResponse(Level Level);

internal sealed record DistanceResponse(Distance Distance);

internal sealed record ScopeResponse(OrderStatus Status, OrderStatus? Optional, int? Count);

internal sealed record TwoStatusesResponse(OrderStatus First, OrderStatus Second);

internal sealed record NullableStatusResponse(OrderStatus? Status);

internal sealed record NullableFirstResponse(OrderStatus? First, OrderStatus Second);

internal sealed record PlainFirstResponse(OrderStatus First, OrderStatus? Second);

internal sealed record StringPropertyResponse(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus Plain,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus? Optional);

internal sealed record StringFirstResponse(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus First,
    OrderStatus Second);

internal sealed record NumberFirstResponse(
    OrderStatus First,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] OrderStatus Second);
