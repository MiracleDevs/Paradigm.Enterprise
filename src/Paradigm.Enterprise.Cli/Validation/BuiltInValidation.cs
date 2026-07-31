namespace Paradigm.Enterprise.Cli;
internal static class BuiltInValidation
{
#region Fields
    private static readonly IReadOnlyList<IValidationRule> Rules = [new DiscoveryConventionRule(), new IdentifierConsistencyRule(), new ControllerAuthorizationRule(), new EntitySetterRule(), new RepositoryQueryableRule()];
#endregion
#region Public Methods
    public static IReadOnlyList<Diagnostic> Evaluate(IEnumerable<InspectedType> applicationTypes, IEnumerable<InspectedType>? universe = null)
    {
        var application = applicationTypes.Where(x => x.IsPublic).ToArray();
        var context = new ValidationContext(application, universe?.ToArray() ?? application);
        return Rules.SelectMany(rule => rule.Evaluate(context)).DistinctBy(x => (x.Code, x.Message, x.Location)).OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Message, StringComparer.Ordinal).ThenBy(x => x.Location, StringComparer.Ordinal).ToArray();
    }
#endregion
}
