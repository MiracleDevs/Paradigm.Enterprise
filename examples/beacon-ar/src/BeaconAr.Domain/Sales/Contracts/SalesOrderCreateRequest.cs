using BeaconAr.Domain.Sales.Application;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesOrderCreateRequest(
    int CustomerId,
    int ShippingAddressId,
    DateOnly? RequestedShipDate,
    int? CarrierId,
    string? TrackingNumber,
    IReadOnlyList<SalesLineRequest> Lines)
{
    #region Public Methods

    public void ValidateReferences()
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (CustomerId <= 0)
            errors["customerId"] = ["Customer ID must be positive."];
        if (ShippingAddressId <= 0)
            errors["shippingAddressId"] = ["Shipping address ID must be positive."];
        if (CarrierId is <= 0)
            errors["carrierId"] = ["Carrier ID must be positive."];
        if (errors.Count > 0)
            throw new SalesValidationException(errors);
    }

    #endregion
}
