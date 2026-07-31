namespace Paradigm.Enterprise.Cli;
internal sealed class ValidationService : IValidationService
{
#region Public Methods
    public IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types) => Analysis.Inspect(types);
    public IReadOnlyList<Diagnostic> ValidateTypes(IEnumerable<InspectedType> application, IEnumerable<InspectedType> all) => Analysis.ValidateTypes(application, all);
    public IReadOnlyList<Diagnostic> ValidateLayers(ProjectSelection selection) => Analysis.ValidateLayers(selection);
#endregion
}
