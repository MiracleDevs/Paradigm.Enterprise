using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public sealed class OracleSixteenResultsProcedure : ResultStoredProcedureBase<OracleInput,
    ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, ReportRow,
    ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, ReportRow, decimal>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleSixteenResults";

    #endregion
}
