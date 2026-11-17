using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlDecimalProcedure : ResultStoredProcedureBase<object?, decimal>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlDecimal";

    #endregion
}