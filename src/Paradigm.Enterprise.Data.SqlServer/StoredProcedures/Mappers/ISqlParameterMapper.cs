using Microsoft.Data.SqlClient;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;

/// <summary>
/// Converts an application parameter object into SQL Server command parameters.
/// </summary>
/// <remarks>
/// Stored-procedure callers dispose the mapper immediately after <see cref="Map"/> returns. Implementations
/// must return parameter objects that remain valid and usable after mapper disposal.
/// </remarks>
public interface ISqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps an application parameter object to SQL Server parameters.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>
    /// The parameters to add to a SQL Server command. They must remain usable after this mapper is disposed.
    /// </returns>
    SqlParameter[] Map(object parameters);
}
