using Paradigm.Enterprise.Data.PostgreSql.StoredProcedures;

namespace MapperFixture;

public sealed class PostgreSqlReportProcedure : ResultStoredProcedureBase<PostgreSqlInput, List<ReportRow>>
{
    #region Overrides

    protected override string StoredProcedureName => "PostgreSqlReport";

    #endregion
}