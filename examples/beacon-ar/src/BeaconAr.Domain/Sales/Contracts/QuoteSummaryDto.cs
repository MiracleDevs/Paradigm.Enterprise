namespace BeaconAr.Domain.Sales.Contracts;

public record QuoteSummaryDto(
    int Id,
    string QuoteNumber,
    int CustomerId,
    string CustomerAccountNumber,
    string CustomerName,
    DateOnly QuoteDate,
    DateOnly ValidUntil,
    QuoteStatus Status,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    int? SalesOrderId,
    int? CreatedByUserId,
    DateTimeOffset CreationDate,
    int? ModifiedByUserId,
    DateTimeOffset? ModificationDate,
    string Version);
