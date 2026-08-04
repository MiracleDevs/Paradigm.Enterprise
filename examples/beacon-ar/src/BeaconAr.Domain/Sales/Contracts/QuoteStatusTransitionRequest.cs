using BeaconAr.Interfaces.Sales.Enums;

namespace BeaconAr.Domain.Sales.Contracts;

public sealed record QuoteStatusTransitionRequest(QuoteStatus Status);
