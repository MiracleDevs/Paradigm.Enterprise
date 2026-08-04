using System.Runtime.Serialization;

namespace BeaconAr.Interfaces.MasterData.Enums;

public enum AddressType
{
    [EnumMember(Value = "billing")]
    Billing = 1,

    [EnumMember(Value = "shipping")]
    Shipping = 2,

    [EnumMember(Value = "both")]
    Both = 3,
}
