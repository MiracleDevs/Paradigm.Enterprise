using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleNonQueryProcedure : StoredProcedureBase<OracleInput>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleArchive";

    #endregion
}
