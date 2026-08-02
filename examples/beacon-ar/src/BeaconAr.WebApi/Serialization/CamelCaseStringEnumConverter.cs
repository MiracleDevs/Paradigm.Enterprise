using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeaconAr.WebApi.Serialization;

public sealed class CamelCaseStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    #region Constructors

    public CamelCaseStringEnumConverter()
        : base(JsonNamingPolicy.CamelCase, false)
    {
    }

    #endregion
}
