using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Sales.Application;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BeaconAr.Data.Sales;

public sealed class SalesPersistenceErrorClassifier : ISalesPersistenceErrorClassifier
{
    #region Public Methods

    public PersistenceConflictKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (Find<DbUpdateConcurrencyException>(exception) is not null)
            return PersistenceConflictKind.Concurrency;
        SqlException? sql = Find<SqlException>(exception);
        if (sql is null)
            return PersistenceConflictKind.None;
        if (sql.Number is 2601 or 2627 && Contains(sql.Message,
                "UQ_Quote_QuoteNumber", "UQ_QuoteLine_QuoteId_ProductId", "UQ_SalesOrder_OrderNumber",
                "UQ_SalesOrderLine_SalesOrderId_ProductId", "UQ_SalesOrder_SourceQuoteId"))
            return PersistenceConflictKind.Duplicate;
        if (sql.Number == 547 && Contains(sql.Message, "FK_Quote_", "FK_QuoteLine_", "FK_SalesOrder_", "FK_SalesOrderLine_"))
            return PersistenceConflictKind.Referenced;
        return PersistenceConflictKind.None;
    }

    public bool IsSourceQuoteSingletonConflict(Exception exception)
    {
        SqlException? sql = Find<SqlException>(exception);
        return sql is not null && sql.Number is 2601 or 2627 &&
               sql.Message.Contains("UQ_SalesOrder_SourceQuoteId", StringComparison.Ordinal);
    }

    #endregion

    #region Private Methods

    private static TException? Find<TException>(Exception exception) where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is TException match)
                return match;
        return null;
    }

    private static bool Contains(string source, params string[] values) =>
        values.Any(value => source.Contains(value, StringComparison.Ordinal));

    #endregion
}
