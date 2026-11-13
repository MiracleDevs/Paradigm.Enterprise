using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlDateTimeProcedure : ResultStoredProcedureBase<object?, DateTime>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlDateTime";

    #endregion
}