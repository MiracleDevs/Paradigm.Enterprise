namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteConversionResult(SalesOrderDto SalesOrder, bool Created);
