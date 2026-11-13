using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlNoParametersProcedure : StoredProcedureBase
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlRefresh";

    #endregion
}