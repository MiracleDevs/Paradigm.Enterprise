using System.Runtime.Serialization;

namespace BeaconAr.Interfaces.Sales.Enums;

public enum QuoteStatus
{
    [EnumMember(Value = "draft")]
    Draft = 1,

    [EnumMember(Value = "sent")]
    Sent = 2,

    [EnumMember(Value = "accepted")]
    Accepted = 3,

    [EnumMember(Value = "rejected")]
    Rejected = 4,

    [EnumMember(Value = "expired")]
    Expired = 5,
}
