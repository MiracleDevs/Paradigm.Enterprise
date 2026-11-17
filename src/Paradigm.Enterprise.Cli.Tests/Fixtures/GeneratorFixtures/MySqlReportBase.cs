using Paradigm.Enterprise.Data.MySql.StoredProcedures;

namespace MapperFixture;

public abstract class MySqlReportBase<TResult> : ResultStoredProcedureBase<Input, List<TResult>>;