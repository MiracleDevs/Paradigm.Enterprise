namespace Paradigm.Enterprise.Cli;

internal sealed class RepositoryQueryableRule : IValidationRule
{
    #region Properties

    public string Code => "PE3102";

    #endregion

    #region Public Methods

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes)
        {
            if (Analysis.Classify(type, context.AllTypes) != "repository")
                continue;
            foreach (var member in type.StructuredMembers?.Where(x => x.Kind is "method" or "property" && x.Signature.StartsWith("public ", StringComparison.Ordinal) && (x.Type?.Contains("System.Linq.IQueryable", StringComparison.Ordinal) == true || x.Signature.Contains("System.Linq.IQueryable", StringComparison.Ordinal))) ?? [])
                yield return new(Code, "error", $"{type.FullName}.{member.Name} exposes IQueryable across the repository boundary.", $"{type.AssemblyName}:{type.FullName}.{member.Name}");
        }
    }

    #endregion
}