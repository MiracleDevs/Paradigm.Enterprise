namespace Paradigm.Enterprise.Cli;

internal sealed record ValidationContext(
    IReadOnlyList<InspectedType> ApplicationTypes,
    IReadOnlyList<InspectedType> AllTypes);

internal interface IValidationRule
{
    string Code { get; }
    IEnumerable<Diagnostic> Evaluate(ValidationContext context);
}

internal static class BuiltInValidation
{
    private static readonly IReadOnlyList<IValidationRule> Rules =
    [
        new DiscoveryConventionRule(),
        new IdentifierConsistencyRule(),
        new ControllerAuthorizationRule(),
        new EntitySetterRule(),
        new RepositoryQueryableRule()
    ];

    public static IReadOnlyList<Diagnostic> Evaluate(
        IEnumerable<InspectedType> applicationTypes,
        IEnumerable<InspectedType>? universe = null)
    {
        var application = applicationTypes.Where(x => x.IsPublic).ToArray();
        var context = new ValidationContext(application, universe?.ToArray() ?? application);
        return Rules.SelectMany(rule => rule.Evaluate(context))
            .DistinctBy(x => (x.Code, x.Message, x.Location))
            .OrderBy(x => x.Code, StringComparer.Ordinal)
            .ThenBy(x => x.Message, StringComparer.Ordinal)
            .ThenBy(x => x.Location, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed class DiscoveryConventionRule : IValidationRule
{
    public string Code => "PE3001";

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes.Where(x => !x.IsAbstract))
        {
            var kind = Analysis.Classify(type, context.AllTypes);
            if (kind is not ("provider" or "repository"))
                continue;
            var expected = "I" + Analysis.StripGenericName(type.Name);
            var count = type.Interfaces.Count(x =>
                Analysis.SimpleTypeName(x).Equals(expected, StringComparison.Ordinal));
            if (count != 1)
                yield return new(Code, "error",
                    $"{type.FullName} must implement exactly one convention interface named {expected}; found {count}.",
                    type.AssemblyName);
        }
    }
}

internal sealed class IdentifierConsistencyRule : IValidationRule
{
    public string Code => "PE3002";

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var capability in context.ApplicationTypes.Where(x => !x.IsAbstract)
                     .SelectMany(type => Analysis.GetIdentifierCandidates(type, context.AllTypes)
                         .Select(id => (Capability: Analysis.GetCapabilityName(type.Name), Type: type, Id: id)))
                     .GroupBy(x => x.Capability, StringComparer.OrdinalIgnoreCase))
        {
            var ids = capability.Select(x => x.Id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (ids.Length > 1)
                yield return new(Code, "error",
                    $"Capability '{capability.Key}' uses inconsistent identifier types: {string.Join(", ", ids)}.",
                    string.Join(", ", capability.Select(x => x.Type.AssemblyName).Distinct().Order()));
        }
    }
}

internal sealed class ControllerAuthorizationRule : IValidationRule
{
    public string Code => "PE4001";

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes.Where(x => !x.IsAbstract))
            if (Analysis.Classify(type, context.AllTypes) == "controller" &&
                Analysis.InheritsAnonymousControllerMetadata(type, context.AllTypes) &&
                !Analysis.HasControllerAuthorization(type, context.AllTypes))
                yield return new(Code, "warning",
                    $"{type.FullName} inherits anonymous controller metadata without an independent authorization filter on its type hierarchy or every inherited action.",
                    type.AssemblyName);
    }
}

internal sealed class EntitySetterRule : IValidationRule
{
    public string Code => "PE3101";

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes)
        {
            if (Analysis.Classify(type, context.AllTypes) != "entity" ||
                HasGeneratedMarker(type.Attributes))
                continue;
            foreach (var property in type.StructuredMembers?.Where(x => x.Kind == "property") ?? [])
            {
                if (property.Name == "Id" ||
                    HasGeneratedMarker(property.Attributes) ||
                    !property.Accessors.Any(x => x.Kind == "set" && x.Visibility == "public"))
                    continue;
                yield return new(Code, "warning",
                    $"{type.FullName}.{property.Name} has a public setter. Handwritten entities should expose intention-revealing behavior and non-public setters.",
                    $"{type.AssemblyName}:{type.FullName}.{property.Name}");
            }
        }
    }

    private static bool HasGeneratedMarker(IEnumerable<string> attributes) =>
        attributes.Any(attribute =>
            attribute.EndsWith(".GeneratedCodeAttribute", StringComparison.Ordinal) ||
            attribute.EndsWith(".CompilerGeneratedAttribute", StringComparison.Ordinal) ||
            attribute is "System.CodeDom.Compiler.GeneratedCodeAttribute" or
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
}

internal sealed class RepositoryQueryableRule : IValidationRule
{
    public string Code => "PE3102";

    public IEnumerable<Diagnostic> Evaluate(ValidationContext context)
    {
        foreach (var type in context.ApplicationTypes)
        {
            if (Analysis.Classify(type, context.AllTypes) != "repository")
                continue;
            foreach (var member in type.StructuredMembers?.Where(x =>
                         x.Kind is "method" or "property" &&
                         x.Signature.StartsWith("public ", StringComparison.Ordinal) &&
                         (x.Type?.Contains("System.Linq.IQueryable", StringComparison.Ordinal) == true ||
                          x.Signature.Contains("System.Linq.IQueryable", StringComparison.Ordinal))) ?? [])
                yield return new(Code, "error",
                    $"{type.FullName}.{member.Name} exposes IQueryable across the repository boundary.",
                    $"{type.AssemblyName}:{type.FullName}.{member.Name}");
        }
    }
}
