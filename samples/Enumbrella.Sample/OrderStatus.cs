using System.ComponentModel;

namespace Enumbrella.Sample;

internal enum OrderStatus
{
    [Description("Waiting for approval")]
    Pending = 0,

    [Description("Approved")]
    Approved = 1,

    Rejected = 2,
}
