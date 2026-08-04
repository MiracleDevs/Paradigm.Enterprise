using System.Data.Common;
using Paradigm.Enterprise.Domain.Repositories;

namespace RepositorySqlTestDouble;

public sealed class RawSqlTestRepository(DbConnection connection) : IRepository
{
    #region Public Methods

    public void Dispose()
    {
    }

    public async Task ExecuteAsync()
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.TestRows";
        await command.ExecuteNonQueryAsync();
    }

    #endregion
}
