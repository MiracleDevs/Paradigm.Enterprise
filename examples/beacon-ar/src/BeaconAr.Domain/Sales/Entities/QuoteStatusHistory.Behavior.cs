using QuoteState = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Sales.Entities;

public partial class QuoteStatusHistory
{
    #region Public Methods

    public static QuoteStatusHistory Create(Quote quote, int actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(quote);
        var rules = new DomainValidator();
        rules.Assert(Enum.IsDefined((QuoteState)quote.StatusId), "Quote history status is invalid.");
        rules.Assert(actorId > 0, "Quote history actor ID must be positive.");
        rules.Assert(now != default, "Quote history creation time is required.");
        rules.ThrowIfAny();
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
