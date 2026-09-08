using BeaconAr.Domain.Operations;
using Microsoft.Data.SqlClient;

namespace BeaconAr.Data.Operations;

public sealed class IdempotencyPersistenceErrorClassifier : IIdempotencyPersistenceErrorClassifier
{
    #region Constants

    private const string KeyConstraint = "UQ_IdempotencyRequest_UserId_Operation_KeyHash";

    #endregion

    #region Public Methods

    public bool IsKeyConflict(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        SqlException? sqlException = Find<SqlException>(exception);
        return sqlException is not null && sqlException.Number is 2601 or 2627 &&
               sqlException.Message.Contains(KeyConstraint, StringComparison.Ordinal);
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

    #endregion
}
