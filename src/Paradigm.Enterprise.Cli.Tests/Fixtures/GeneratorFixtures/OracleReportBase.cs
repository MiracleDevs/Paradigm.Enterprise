using Paradigm.Enterprise.Data.Oracle.StoredProcedures;

namespace MapperFixture;

public abstract class OracleReportBase<TResult> : ResultStoredProcedureBase<OracleInput, List<TResult>>;
