using BeaconAr.Domain.Sales.Application;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteCreateRequest(
    int CustomerId,
    int ShippingAddressId,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    string? Notes,
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
        if (errors.Count > 0)
            throw new SalesValidationException(errors);
    }

    #endregion
}
