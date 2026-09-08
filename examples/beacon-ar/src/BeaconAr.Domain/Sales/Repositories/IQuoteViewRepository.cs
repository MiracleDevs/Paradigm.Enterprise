using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface IQuoteViewRepository : IRepository
{
    Task<QuoteDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<PageResult<QuoteView>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken);
}
