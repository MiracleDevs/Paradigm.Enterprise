namespace BeaconAr.Domain.Sales.Contracts;

public record SalesOrderSummaryDto(
    int Id,
    string OrderNumber,
    int? SourceQuoteId,
    int CustomerId,
    string CustomerAccountNumber,
    string CustomerName,
    SalesOrderStatus Status,
    DateOnly? RequestedShipDate,
    int? CarrierId,
    string? CarrierName,
    string? TrackingNumber,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
