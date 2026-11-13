using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlPrimitiveListProcedure : ResultStoredProcedureBase<object?, List<int>>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlPrimitiveList";

    #endregion
}