using BeaconAr.Domain.MasterData.Contracts;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record SalesOrderSearchRequest(
    string? Search = null,
    SalesOrderStatus? Status = null,
    int? CustomerId = null,
    int? SourceQuoteId = null,
    int PageNumber = 1,
    int PageSize = 10,
    string? SortField = null,
    SortDirection SortDirection = SortDirection.Desc);
