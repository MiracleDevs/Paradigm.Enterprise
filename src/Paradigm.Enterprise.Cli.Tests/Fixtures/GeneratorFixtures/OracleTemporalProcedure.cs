using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleTemporalProcedure : ResultStoredProcedureBase<object?, DateTimeOffset, TimeSpan>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleTemporal";

    #endregion
}
