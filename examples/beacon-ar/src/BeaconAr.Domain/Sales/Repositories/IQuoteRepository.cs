using BeaconAr.Domain.Receivables.Generated;
using Paradigm.Enterprise.Domain.Repositories;

namespace BeaconAr.Domain.Sales.Repositories;

public interface IQuoteRepository : IRepository
{
    Task<string> AllocateNumberAsync(CancellationToken cancellationToken);

    Task<Quote?> GetForUpdateAsync(int id, CancellationToken cancellationToken);

    Task<Quote?> LockForConversionAsync(int id, CancellationToken cancellationToken);

    void Add(Quote quote);

    void ReplaceLines(Quote quote, IReadOnlyCollection<QuoteLine> lines);

    void AddHistory(QuoteStatusHistory history);
}
