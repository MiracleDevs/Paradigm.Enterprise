namespace MapperFixture;

public sealed class OracleReportProcedure : OracleReportBase<ReportRow>
{
    #region Overrides

    protected override string StoredProcedureName => "OracleReport";

    #endregion
}
