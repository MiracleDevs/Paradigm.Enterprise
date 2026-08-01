using BeaconAr.Domain.MasterData.Contracts;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteSearchRequest(
    string? Search = null,
    QuoteStatus? Status = null,
    int? CustomerId = null,
    int PageNumber = 1,
    int PageSize = 10,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Desc);
