using System.Text.Json.Serialization.Metadata;
using BeaconAr.Domain.MasterData.Entities;

namespace BeaconAr.WebApi.Serialization;

public static class BeaconArApiJsonContract
{
    #region Public Methods

    public static IJsonTypeInfoResolver CreateResolver() =>
        BeaconArApiJsonContext.Default.WithAddedModifier(Configure);

    public static void Configure(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type != typeof(ProductView) &&
            typeInfo.Type != typeof(CustomerView) &&
            typeInfo.Type != typeof(CustomerAddressView) &&
            typeInfo.Type != typeof(CarrierView))
            return;

        Rename(typeInfo, "rowVersion", "version");
        if (typeInfo.Type == typeof(CustomerAddressView))
        {
            Rename(typeInfo, "addressTypeCode", "type");
            Rename(typeInfo, "isDefaultBilling", "defaultBilling");
            Rename(typeInfo, "isDefaultShipping", "defaultShipping");
        }
    }

    #endregion

    #region Private Methods

    private static void Rename(JsonTypeInfo typeInfo, string currentName, string transportName)
    {
        JsonPropertyInfo? property = typeInfo.Properties.SingleOrDefault(property =>
            string.Equals(property.Name, currentName, StringComparison.Ordinal));
        if (property is not null)
            property.Name = transportName;
    }

    #endregion
}
