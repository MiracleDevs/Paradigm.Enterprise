using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public sealed class MySqlScalarProcedure : ResultStoredProcedureBase<object?, long>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlCount";

    #endregion
}