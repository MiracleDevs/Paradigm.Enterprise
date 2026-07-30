using Npgsql;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;

/// <summary>
/// Converts an application parameter object into PostgreSQL command parameters.
/// </summary>
public interface INpgsqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps an application parameter object to PostgreSQL parameters.
    /// </summary>
    /// <param name="parameters">The application parameter object to map.</param>
    /// <returns>The parameters to add to an Npgsql command.</returns>
    NpgsqlParameter[] Map(object parameters);
}
