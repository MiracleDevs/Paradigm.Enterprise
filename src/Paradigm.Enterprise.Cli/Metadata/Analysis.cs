using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;
internal static class Analysis
{
#region Public Methods
    public static IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types)
    {
        var materialized = types.ToArray();
        return materialized.Where(x => x.IsPublic && !x.IsAbstract).Select(x => Classify(x, materialized)is { } kind ? new ResultItem(kind, x.FullName, IdentityDetail(x, materialized), x.Package, x.Version, x.AssemblyName) : null).Where(x => x is not null).Cast<ResultItem>().OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<Diagnostic> ValidateTypes(IEnumerable<InspectedType> applicationTypes, IEnumerable<InspectedType>? universe = null) => BuiltInValidation.Evaluate(applicationTypes, universe);
    public static IReadOnlyList<Diagnostic> ValidateLayers(ProjectSelection selection)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var project in selection.Projects)
        {
            var source = Layer(Path.GetFileNameWithoutExtension(project));
            if (source is null)
                continue;
            XDocument document;
            try
            {
                document = XDocument.Load(project);
            }
            catch (Exception exception)when (exception is IOException or System.Xml.XmlException)
            {
                diagnostics.Add(new("PE1002", "error", $"Project '{project}' could not be read: {exception.Message}", project));
                continue;
            }

            foreach (var reference in document.Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (include is null)
                    continue;
                var targetName = Path.GetFileNameWithoutExtension(include);
                var target = Layer(targetName);
                if (target is not null && target.Value.Rank > source.Value.Rank)
                    diagnostics.Add(new("PE2001", "error", $"{Path.GetFileNameWithoutExtension(project)} ({source.Value.Name}) references {targetName} ({target.Value.Name}) against the canonical dependency direction.", project));
            }
        }

        return diagnostics;
    }

    public static string? Classify(InspectedType type, IReadOnlyList<InspectedType>? universe = null)
    {
        if (type.Interfaces.Any(x => ContainsType(x, "Paradigm.Enterprise.Domain.Repositories.IRepository")))
            return "repository";
        if (type.Interfaces.Any(x => ContainsType(x, "Paradigm.Enterprise.Providers.IProvider")))
            return "provider";
        if (Ancestry(type, universe ?? [type]).Any(x => DefinitionName(x.FullName).Equals("Microsoft.AspNetCore.Mvc.ControllerBase", StringComparison.Ordinal) || DefinitionName(x.FullName).StartsWith("Paradigm.Enterprise.WebApi.Controllers.", StringComparison.Ordinal)))
            return "controller";
        if (type.Interfaces.Any(x => ContainsType(x, "Paradigm.Enterprise.Interfaces.IEntity")))
            return type.Name.EndsWith("View", StringComparison.Ordinal) ? "view" : "entity";
        if (type.Name.EndsWith("View", StringComparison.Ordinal))
            return "view";
        return null;
    }

#endregion
#region Private Methods
    private static string IdentityDetail(InspectedType type, IReadOnlyList<InspectedType> all)
    {
        var ids = IdentifierCandidates(type, all).Distinct().ToArray();
        var detail = ids.Length == 0 ? type.BaseType ?? "" : $"id={string.Join("|", ids)}; base={type.BaseType}";
        if (Classify(type, all) == "controller")
        {
            var chain = Ancestry(type, all).ToArray();
            var typeAuth = chain.SelectMany(x => x.Attributes).Any(IsIndependentAuthorization);
            var actionCount = chain.SelectMany(x => x.Actions).Count();
            detail += $"; authorization={(typeAuth ? "independent-type-filter" : "action-or-none")}; actions={actionCount}";
        }

        return detail.TrimStart(';', ' ');
    }

    internal static IEnumerable<string> GetIdentifierCandidates(InspectedType type, IReadOnlyList<InspectedType> all) => IdentifierCandidates(type, all);
    private static IEnumerable<string> IdentifierCandidates(InspectedType type, IReadOnlyList<InspectedType> all)
    {
        foreach (var contract in type.Interfaces.Append(type.BaseType ?? ""))
        {
            var definition = DefinitionName(contract);
            var simpleDefinition = SimpleName(definition);
            var args = GenericArguments(contract);
            if (simpleDefinition == "IEntity")
            {
                yield return args.Count == 1 ? args[0] : "System.Int32";
                continue;
            }

            if (simpleDefinition == "EntityBase")
            {
                if (args.Count is 1 or 4)
                    yield return args[0];
                else if (args.Count is 0 or 3)
                    yield return "System.Int32";
                continue;
            }

            var currentArity = simpleDefinition switch
            {
                "IReadRepository" or "IEditRepository" or "IReadProvider" or "IEditProvider" => 2,
                "ReadRepositoryBase" or "EditRepositoryBase" => 3,
                "ReadProviderBase" => 4,
                "EditProviderBase" => 6,
                "ReadApiControllerBase" or "EditApiControllerBase" => 4,
                _ => -1
            };
            if (currentArity > 0 && args.Count == currentArity)
                yield return args[^1];
            else if (currentArity > 0 && args.Count == currentArity - 1)
                yield return "System.Int32";
        }
    }

    private static IReadOnlyList<string> GenericArguments(string value)
    {
        var start = value.IndexOf('<');
        var end = value.LastIndexOf('>');
        if (start < 0 || end <= start)
            return[];
        var result = new List<string>();
        var depth = 0;
        var segment = start + 1;
        for (var i = start + 1; i < end; i++)
        {
            if (value[i] == '<')
                depth++;
            if (value[i] == '>')
                depth--;
            if (value[i] == ',' && depth == 0)
            {
                result.Add(value[segment..i].Trim());
                segment = i + 1;
            }
        }

        result.Add(value[segment..end].Trim());
        return result;
    }

    internal static bool InheritsAnonymousControllerMetadata(InspectedType type, IReadOnlyList<InspectedType> all) => HasAnonymousControllerMetadata(type, all);
    private static bool HasAnonymousControllerMetadata(InspectedType type, IReadOnlyList<InspectedType> all) => Ancestry(type, all).Any(x => x.Attributes.Any(attribute => attribute.EndsWith(".AllowAnonymousAttribute", StringComparison.Ordinal)) || DefinitionName(x.FullName).StartsWith("Paradigm.Enterprise.WebApi.Controllers.", StringComparison.Ordinal));
    internal static bool HasControllerAuthorization(InspectedType type, IReadOnlyList<InspectedType> all) => HasIndependentAuthorization(type, all);
    private static bool HasIndependentAuthorization(InspectedType type, IReadOnlyList<InspectedType> all)
    {
        var chain = Ancestry(type, all).ToArray();
        if (chain.SelectMany(x => x.Attributes).Any(IsIndependentAuthorization))
            return true;
        var actions = chain.SelectMany(x => x.Actions).ToArray();
        return actions.Length > 0 && actions.All(action => action.Attributes.Any(IsIndependentAuthorization));
    }

    private static IEnumerable<InspectedType> Ancestry(InspectedType type, IReadOnlyList<InspectedType> all)
    {
        var current = type;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (visited.Add(DefinitionName(current.FullName)))
        {
            yield return current;
            if (current.BaseType is null)
                yield break;
            var baseDefinition = DefinitionName(current.BaseType);
            var next = all.FirstOrDefault(candidate => DefinitionName(candidate.FullName).Equals(baseDefinition, StringComparison.Ordinal));
            if (next is null)
            {
                yield return current with
                {
                    FullName = current.BaseType,
                    Name = SimpleName(current.BaseType),
                    BaseType = null,
                    Interfaces = [],
                    Attributes = [],
                    Members = [],
                    Actions = []
                };
                yield break;
            }

            current = next;
        }
    }

    private static bool IsIndependentAuthorization(string attribute) => attribute is "Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute";
    private static bool ContainsType(string value, string name) => value.Equals(name, StringComparison.Ordinal) || value.StartsWith(name + "<", StringComparison.Ordinal);
    private static string DefinitionName(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }

    internal static string SimpleTypeName(string value) => SimpleName(value);
    private static string SimpleName(string value)
    {
        var name = DefinitionName(value).Split('.').Last();
        return StripGeneric(name);
    }

    internal static string StripGenericName(string value) => StripGeneric(value);
    private static string StripGeneric(string value)
    {
        var index = value.IndexOfAny(['`', '<']);
        return index < 0 ? value : value[..index];
    }

    internal static string GetCapabilityName(string value) => CapabilityName(value);
    private static string CapabilityName(string value)
    {
        var name = StripGeneric(value);
        foreach (var suffix in new[]
        {
            "ViewRepository",
            "Repository",
            "ReadProvider",
            "EditProvider",
            "Provider",
            "Controller",
            "Entity",
            "View"
        }

        )
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name[..^suffix.Length];
        return name;
    }

    private static (int Rank, string Name)? Layer(string projectName)
    {
        if (projectName.EndsWith(".Interfaces", StringComparison.OrdinalIgnoreCase))
            return (0, "Interfaces");
        if (projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase))
            return (1, "Domain");
        if (projectName.EndsWith(".Data", StringComparison.OrdinalIgnoreCase) || projectName.Contains(".Data.", StringComparison.OrdinalIgnoreCase))
            return (2, "Data");
        if (projectName.EndsWith(".Providers", StringComparison.OrdinalIgnoreCase))
            return (3, "Providers");
        if (projectName.EndsWith(".WebApi", StringComparison.OrdinalIgnoreCase) || projectName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase) || projectName.EndsWith(".Host", StringComparison.OrdinalIgnoreCase))
            return (4, "WebApi/Host");
        return null;
    }
#endregion
}
