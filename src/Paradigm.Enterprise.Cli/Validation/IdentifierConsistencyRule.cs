namespace Paradigm.Enterprise.Cli;

internal sealed class IdentifierConsistencyRule : IValidationRule
{
    #region Properties

    public string Code => "PE3002";

    #endregion

    #region Public Methods

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var capability in context.ApplicationTypes.Where(x => !x.IsAbstract).SelectMany(type => Analysis.GetIdentifierCandidates(type, context.AllTypes).Select(id => (Capability: Analysis.GetCapabilityName(type.Name), Type: type, Id: id))).GroupBy(x => x.Capability, StringComparer.OrdinalIgnoreCase))
        {
            var ids = capability.Select(x => x.Id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (ids.Length > 1)
                yield return new(Code, "error", $"Capability '{capability.Key}' uses inconsistent identifier types: {string.Join(", ", ids)}.", string.Join(", ", capability.Select(x => x.Type.AssemblyName).Distinct().Order()));
        }
    }

    #endregion
}