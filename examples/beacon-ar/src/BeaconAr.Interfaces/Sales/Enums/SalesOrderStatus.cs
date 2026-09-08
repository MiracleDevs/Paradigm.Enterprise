using System.Runtime.Serialization;

namespace BeaconAr.Interfaces.Sales.Enums;

public enum SalesOrderStatus
{
    [EnumMember(Value = "draft")]
    Draft = 1,

    [EnumMember(Value = "confirmed")]
    Confirmed = 2,

    [EnumMember(Value = "processing")]
    Processing = 3,

    [EnumMember(Value = "shipped")]
    Shipped = 4,

    [EnumMember(Value = "completed")]
    Completed = 5,

    [EnumMember(Value = "cancelled")]
    Cancelled = 6,
}
