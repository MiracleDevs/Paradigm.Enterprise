using Npgsql;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;

/// <summary>
/// Converts an application parameter object into PostgreSQL command parameters.
/// </summary>
/// <remarks>
/// Stored-procedure callers dispose the mapper immediately after <see cref="Map"/> returns. Implementations
/// must return parameter objects that remain valid and usable after mapper disposal.
/// </remarks>
public interface INpgsqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps an application parameter object to PostgreSQL parameters.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>
    /// The parameters to add to an Npgsql command. They must remain usable after this mapper is disposed.
    /// </returns>
    NpgsqlParameter[] Map(object parameters);
}
