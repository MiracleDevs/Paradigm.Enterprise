namespace BeaconAr.Domain.Receivables.Entities;

public partial class QuoteStatusHistory
{
    #region Public Methods

    public static QuoteStatusHistory Create(Quote quote, int actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new QuoteStatusHistory
        {
            Quote = quote,
            StatusId = quote.StatusId,
            CreatedByUserId = actorId,
            CreationDate = now,
        };
    }

    #endregion
}
