using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlNonQueryProcedure : StoredProcedureBase<Input>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlArchive";

    #endregion
}