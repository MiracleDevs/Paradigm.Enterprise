using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;

namespace MapperFixture;

public sealed class SqlServerReportProcedure : ResultStoredProcedureBase<SqlServerInput, List<ReportRow>>
{
    #region Overrides

    protected override string StoredProcedureName => "SqlServerReport";

    #endregion
}