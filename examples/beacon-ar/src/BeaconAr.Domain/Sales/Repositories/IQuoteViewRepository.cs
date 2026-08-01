using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface IQuoteViewRepository : IRepository
{
    Task<QuoteDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<QuoteSummaryDto>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken);
}
