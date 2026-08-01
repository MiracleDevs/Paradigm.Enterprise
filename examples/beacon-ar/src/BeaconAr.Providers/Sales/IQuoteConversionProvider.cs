using BeaconAr.Domain.Sales.Contracts;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Sales;

public interface IQuoteConversionProvider : IProvider
{
    Task<QuoteConversionResult> ConvertAsync(int quoteId, string expectedVersion, CancellationToken cancellationToken);
}
