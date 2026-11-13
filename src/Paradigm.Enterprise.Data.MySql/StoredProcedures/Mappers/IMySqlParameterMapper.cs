using MySql.Data.MySqlClient;

namespace Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;

/// <summary>Maps an application parameter object to independent Oracle MySQL parameters.</summary>
public interface IMySqlParameterMapper : IDisposable
{
    /// <summary>Creates parameters for one command execution.</summary>
    /// <param name="parameters">The application parameter object.</param>
    /// <returns>Parameters that remain valid after mapper disposal and are not shared with another command.</returns>
    MySqlParameter[] Map(object parameters);
}