using Microsoft.Data.SqlClient;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;

/// <summary>
/// Converts an application parameter object into SQL Server command parameters.
/// </summary>
public interface ISqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps an application parameter object to SQL Server parameters.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>The parameters to add to a SQL Server command.</returns>
    SqlParameter[] Map(object parameters);
}
