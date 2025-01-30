using Microsoft.Data.SqlClient;

namespace Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;

public interface ISqlParameterMapper : IDisposable
{
    /// <summary>
    /// Maps the specified parameters.
    /// </summary>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    SqlParameter[] Map(object parameters);
}