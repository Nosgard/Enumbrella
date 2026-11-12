using System.ComponentModel;

namespace Enumbrella.Tests.Descriptions;

// Kept apart from the sample's OrderStatus: in one document both would map to one component.
internal enum OrderStatus
{
    [Description("Waiting for approval")]
    Pending = 0,

    [Description("Approved")]
    Approved = 1,

    Rejected = 2,
}

internal enum SpecialText
{
    [Description("Size ≥ 10 ✓ café")]
    Size = 1,

    [Description("**bold** <script>alert(1)</script> \"quoted\" back\\slash\nnext line")]
    Markup = 2,
}

internal enum Switch
{
    Active = 1,

    // Enabled has the value 1 too; naming Active instead of the literal keeps CA1069 quiet.
    [Description("Switched on")]
    Enabled = Active,

    Disabled = 2,
}

internal enum Shipping
{
    [Description("Express delivery")]
    Express = 3,

    [Description("Standard delivery")]
    Standard = 1,

    [Description("Pickup")]
    Pickup = 2,
}

internal enum Channel
{
    [Description(null!)]
    Mail = 1,

    [Description("By phone")]
    Phone = 2,
}

internal enum Labelled
{
    [Description("A")]
    [Label("first label")]
    ExactFirst = 1,

    [Label("second label")]
    [Description("A")]
    ExactLast = 2,

    [Translated("only")]
    DerivedOnly = 3,

    [Label("one")]
    [Translated("two")]
    TwoDerived = 4,

    [MissingText]
    NullText = 5,
}

internal enum Localized
{
    [Translated("card")]
    Card = 1,

    Cash = 2,
}

internal sealed class LabelAttribute(string text) : DescriptionAttribute(text);

// Overrides Description the way attributes for translated texts do.
internal sealed class TranslatedAttribute(string key) : DescriptionAttribute
{
    public override string Description => $"Translated {key}";
}

internal sealed class MissingTextAttribute : DescriptionAttribute
{
    public override string Description => null!;
}

internal sealed record StatusDescriptionsResponse(OrderStatus Status);

internal sealed record SpecialTextResponse(SpecialText Text);

internal sealed record SwitchResponse(Switch Switch);

internal sealed record ShippingAndChannelResponse(Shipping Shipping, Channel Channel);

internal sealed record LabelledResponse(Labelled Labelled, Localized Localized);
