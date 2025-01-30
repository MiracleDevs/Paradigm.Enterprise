using Npgsql;

namespace Paradigm.Enterprise.Data.PostgreSql.StoredProcedures.Mappers;

public interface INpgsqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    NpgsqlParameter[] Map(object parameters);
}