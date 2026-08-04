using System.Runtime.Serialization;

namespace BeaconAr.Interfaces.Operations.Enums;

public enum IdempotencyState
{
    [EnumMember(Value = "in_progress")]
    InProgress = 1,

    [EnumMember(Value = "completed")]
    Completed = 2,

    [EnumMember(Value = "failed")]
    Failed = 3,
}
