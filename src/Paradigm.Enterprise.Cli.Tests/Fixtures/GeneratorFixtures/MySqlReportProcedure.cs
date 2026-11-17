namespace MapperFixture;

public sealed class MySqlReportProcedure : MySqlReportBase<ReportRow>
{
    #region Overrides

    protected override string StoredProcedureName => "MySqlReport";

    #endregion
}