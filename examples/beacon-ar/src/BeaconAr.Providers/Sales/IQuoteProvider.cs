using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using Paradigm.Enterprise.Providers;

namespace BeaconAr.Providers.Sales;

public interface IQuoteProvider : IProvider
{
    Task<PageResult<QuoteView>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken);
    Task<QuoteDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<QuoteDto> CreateAsync(QuoteCreateRequest request, CancellationToken cancellationToken);
    Task<QuoteDto> UpdateAsync(int id, QuoteUpdateRequest request, string expectedVersion, CancellationToken cancellationToken);
    Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken);
    Task<QuoteDto> TransitionAsync(int id, QuoteStatusTransitionRequest request, string expectedVersion, CancellationToken cancellationToken);
}
