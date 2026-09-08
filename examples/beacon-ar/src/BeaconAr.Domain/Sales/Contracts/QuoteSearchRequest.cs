using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Interfaces.Sales.Enums;
using BeaconAr.Domain.Sales.Application;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteSearchRequest(
    string? Search = null,
    QuoteStatus? Status = null,
    int? CustomerId = null,
    int PageNumber = 1,
    int PageSize = 10,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Desc)
{
    #region Public Methods

    public void Validate()
    {
        Dictionary<string, IReadOnlyList<string>> errors = new(StringComparer.Ordinal);
        if (Search?.Trim().Length > 320)
            errors["search"] = ["Search cannot exceed 320 characters."];
        if (PageNumber <= 0)
            errors["pageNumber"] = ["Page number must be positive."];
        if (PageSize is <= 0 or > 100)
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        if (CustomerId is <= 0)
            errors["customerId"] = ["Customer ID must be positive."];
        if (!Enum.IsDefined(SortDirection))
            errors["sortDirection"] = ["Sort direction is invalid."];
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
            errors["status"] = ["Status is invalid."];
        if (!string.IsNullOrWhiteSpace(SortField) &&
            !new[] { "quoteNumber", "quoteDate", "validUntil", "status" }
                .Contains(SortField.Trim(), StringComparer.OrdinalIgnoreCase))
            errors["sortField"] = ["Sort field is invalid."];
        if (errors.Count > 0)
            throw new SalesValidationException(errors);
    }

    #endregion
}
