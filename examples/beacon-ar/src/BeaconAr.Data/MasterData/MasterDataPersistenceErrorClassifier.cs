using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BeaconAr.Data.MasterData;

public sealed class MasterDataPersistenceErrorClassifier : IMasterDataPersistenceErrorClassifier
{
    #region Public Methods

    public PersistenceConflictKind Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (Find<DbUpdateConcurrencyException>(exception) is not null)
            return PersistenceConflictKind.Concurrency;

        SqlException? sqlException = Find<SqlException>(exception);
        if (sqlException is null)
            return PersistenceConflictKind.None;

        if (sqlException.Number is 2601 or 2627 && ContainsReviewedUniqueConstraint(sqlException.Message))
            return PersistenceConflictKind.Duplicate;
        if (sqlException.Number == 547 && ContainsReviewedReferenceConstraint(sqlException.Message))
            return PersistenceConflictKind.Referenced;

        return PersistenceConflictKind.None;
    }

    #endregion

    #region Private Methods

    private static TException? Find<TException>(Exception exception) where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException match)
                return match;
        }

        return null;
    }

    private static bool ContainsReviewedUniqueConstraint(string message) =>
        message.Contains("UQ_Product_Sku", StringComparison.Ordinal) ||
        message.Contains("UQ_Customer_AccountNumber", StringComparison.Ordinal) ||
        message.Contains("UQ_Carrier_Code", StringComparison.Ordinal) ||
        message.Contains("UQ_CustomerAddress_CustomerId_DefaultBilling", StringComparison.Ordinal) ||
        message.Contains("UQ_CustomerAddress_CustomerId_DefaultShipping", StringComparison.Ordinal);

    private static bool ContainsReviewedReferenceConstraint(string message) =>
        message.Contains("FK_QuoteLine_Product", StringComparison.Ordinal) ||
        message.Contains("FK_SalesOrderLine_Product", StringComparison.Ordinal) ||
        message.Contains("FK_CustomerAddress_Customer", StringComparison.Ordinal) ||
        message.Contains("FK_Quote_Customer", StringComparison.Ordinal) ||
        message.Contains("FK_SalesOrder_Customer", StringComparison.Ordinal) ||
        message.Contains("FK_Quote_CustomerAddress", StringComparison.Ordinal) ||
        message.Contains("FK_SalesOrder_CustomerAddress", StringComparison.Ordinal) ||
        message.Contains("FK_SalesOrder_Carrier", StringComparison.Ordinal);

    #endregion
}
