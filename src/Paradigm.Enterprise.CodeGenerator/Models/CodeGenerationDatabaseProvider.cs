namespace Paradigm.Enterprise.CodeGenerator;

/// <summary>Selects the database adapter used by stored-procedure mapper generation.</summary>
internal enum CodeGenerationDatabaseProvider
{
    SqlServer,
    PostgreSql,
    MySql
}