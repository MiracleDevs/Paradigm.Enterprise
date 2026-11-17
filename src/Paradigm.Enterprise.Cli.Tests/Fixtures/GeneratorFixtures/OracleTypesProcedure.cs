using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleTypesProcedure : ResultStoredProcedureBase<OracleInput, OracleTypesRow>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleTypes";

    #endregion
}
