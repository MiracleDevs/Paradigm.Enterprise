using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlNullableIntProcedure : ResultStoredProcedureBase<object?, int?>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlNullableInt";

    #endregion
}