using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleGuidProcedure : ResultStoredProcedureBase<object?, Guid>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleGuid";

    #endregion
}
