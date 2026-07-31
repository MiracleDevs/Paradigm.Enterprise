namespace Paradigm.Enterprise.Cli;
internal interface IValidationService
{
    IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types);
    IReadOnlyList<Diagnostic> ValidateTypes(IEnumerable<InspectedType> application, IEnumerable<InspectedType> all);
    IReadOnlyList<Diagnostic> ValidateLayers(ProjectSelection selection);
}
