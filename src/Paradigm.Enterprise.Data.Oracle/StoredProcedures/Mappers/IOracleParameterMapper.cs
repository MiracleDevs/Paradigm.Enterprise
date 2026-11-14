using Oracle.ManagedDataAccess.Client;

namespace Paradigm.Enterprise.Data.Oracle.StoredProcedures.Mappers;

/// <summary>Maps an application parameter object to independent Oracle parameters.</summary>
public interface IOracleParameterMapper : IDisposable
{
    /// <summary>Creates parameters for one command execution.</summary>
    /// <param name="parameters">The application parameter object.</param>
    /// <returns>Parameters that remain valid after mapper disposal and are not shared with another command.</returns>
    OracleParameter[] Map(object parameters);
}