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

internal sealed record NullableStatusResponse(OrderStatus? Status);

internal sealed record TwoStatusesResponse(OrderStatus First, OrderStatus Second);
