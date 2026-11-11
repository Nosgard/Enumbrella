using System.ComponentModel;

namespace Enumbrella.Sample;

internal enum PaymentMethod
{
    [Description("Paid by card")]
    Card = 0,

    [Description("Paid on invoice")]
    Invoice = 1,

    [Description("Paid by bank transfer")]
    BankTransfer = 2,
}
