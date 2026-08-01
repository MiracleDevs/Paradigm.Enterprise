using BeaconAr.Data.Receivables;
using BeaconAr.Domain.Receivables.Generated;
using BeaconAr.Domain.Sales.Repositories;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;
using System.Data;

namespace BeaconAr.Data.Sales;

public sealed class QuoteRepository : RepositoryBase<ReceivablesDbContext, int>, IQuoteRepository
{
    #region Constructors

    public QuoteRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public async Task<string> AllocateNumberAsync(CancellationToken cancellationToken)
    {
        var connection = GetDbConnection();
        if (connection.State == ConnectionState.Closed)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NEXT VALUE FOR [dbo].[QuoteNumberSequence]";
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 30;
        UnitOfWork.UseTransaction(command);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        long value = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
        return SalesNumberFormatter.Quote(value);
    }

    public Task<Quote?> GetForUpdateAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Quotes.Include(quote => quote.QuoteLines)
            .SingleOrDefaultAsync(quote => quote.Id == id && quote.DeletionDate == null, cancellationToken);

    public Task<Quote?> LockForConversionAsync(int id, CancellationToken cancellationToken) =>
        EntityContext.Quotes
            .FromSqlInterpolated($"SELECT * FROM [dbo].[Quote] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id} AND [DeletionDate] IS NULL")
            .Include(quote => quote.QuoteLines)
            .SingleOrDefaultAsync(cancellationToken);

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
