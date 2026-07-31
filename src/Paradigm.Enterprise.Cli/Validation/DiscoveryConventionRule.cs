namespace Paradigm.Enterprise.Cli;
internal sealed class DiscoveryConventionRule : IValidationRule
{
#region Properties
    public string Code => "PE3001";

#endregion
#region Public Methods
    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes.Where(x => !x.IsAbstract))
        {
            var kind = Analysis.Classify(type, context.AllTypes);
            if (kind is not ("provider" or "repository"))
                continue;
            var expected = "I" + Analysis.StripGenericName(type.Name);
            var count = type.Interfaces.Count(x => Analysis.SimpleTypeName(x).Equals(expected, StringComparison.Ordinal));
            if (count != 1)
                yield return new(Code, "error", $"{type.FullName} must implement exactly one convention interface named {expected}; found {count}.", type.AssemblyName);
        }
    }
#endregion
}
