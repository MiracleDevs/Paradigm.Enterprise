namespace Paradigm.Enterprise.Cli;
internal sealed class ControllerAuthorizationRule : IValidationRule
{
#region Properties
    public string Code => "PE4001";

#endregion
#region Public Methods
    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes.Where(x => !x.IsAbstract))
            if (Analysis.Classify(type, context.AllTypes) == "controller" && Analysis.InheritsAnonymousControllerMetadata(type, context.AllTypes) && !Analysis.HasControllerAuthorization(type, context.AllTypes))
                yield return new(Code, "warning", $"{type.FullName} inherits anonymous controller metadata without an independent authorization filter on its type hierarchy or every inherited action.", type.AssemblyName);
    }
#endregion
}
