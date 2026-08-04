using BeaconAr.Data.Sales.Context;
using BeaconAr.Data.Sales.StoredProcedures;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Domain.Sales.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Sales.Repositories;

public sealed class QuoteRepository : RepositoryBase<SalesDbContext, int>, IQuoteRepository
{
    #region Fields

    private static readonly AllocateQuoteNumberProcedure AllocateNumberProcedure = new();
    private static readonly LockQuoteForConversionProcedure LockForConversionProcedure = new();

    #endregion

    #region Constructors

    public QuoteRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        long value = await AllocateNumberProcedure.ExecuteAsync(GetDbConnection(), new AllocateQuoteNumberParameters(), UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return SalesNumberFormatter.Quote(value);
    }

    public Task<Quote?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Quotes.Include(quote => quote.QuoteLines)
            .SingleOrDefaultAsync(quote => quote.Id == id && quote.DeletionDate == null, cancellationToken);

    public async Task<Quote?> LockForConversionAsync(int id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int lockedId = await LockForConversionProcedure.ExecuteAsync(GetDbConnection(), new LockQuoteForConversionParameters { Id = id }, UnitOfWork);
        cancellationToken.ThrowIfCancellationRequested();
        return lockedId == 0
            ? null
            : await EntityContext.Quotes.Include(quote => quote.QuoteLines)
                .SingleAsync(quote => quote.Id == lockedId, cancellationToken);
    }

    public void Add(Quote quote) => EntityContext.Quotes.Add(quote);

    public void ReplaceLines(Quote quote, IReadOnlyCollection<QuoteLine> lines)
    {
        EntityContext.QuoteLines.RemoveRange(quote.QuoteLines);
        quote.QuoteLines.Clear();
        foreach (QuoteLine line in lines)
            quote.QuoteLines.Add(line);
    }

    public void AddHistory(QuoteStatusHistory history) => EntityContext.QuoteStatusHistories.Add(history);

    #endregion
}
