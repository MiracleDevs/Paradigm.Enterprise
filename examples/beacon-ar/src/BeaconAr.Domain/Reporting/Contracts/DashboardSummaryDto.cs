namespace BeaconAr.Domain.Reporting.Contracts;

public sealed record DashboardSummaryDto(
    long Products,
    long Customers,
    long Carriers,
    long OpenQuotes,
    long ActiveOrders,
    DateTimeOffset AsOf);
