using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlGuidProcedure : ResultStoredProcedureBase<object?, Guid>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlGuid";

    #endregion
}