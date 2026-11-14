using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleNoParametersProcedure : StoredProcedureBase
{
    #region Overrides

    protected override string StoredProcedureName => "OracleRefresh";

    #endregion
}
