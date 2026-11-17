using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlStringProcedure : ResultStoredProcedureBase<object?, string>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlString";

    #endregion
}