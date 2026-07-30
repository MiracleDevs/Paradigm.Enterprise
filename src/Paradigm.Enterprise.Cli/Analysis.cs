using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal static class Analysis
{
    public static IReadOnlyList<ResultItem> Inspect(IEnumerable<InspectedType> types)
    {
        var materialized = types.ToArray();
        return materialized.Where(x => x.IsPublic && !x.IsAbstract)
            .Select(x => Classify(x, materialized) is { } kind
                ? new ResultItem(kind, x.FullName, IdentityDetail(x, materialized), x.Package, x.Version, x.AssemblyName)
                : null)
            .Where(x => x is not null)
            .Cast<ResultItem>()
            .OrderBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<Diagnostic> ValidateTypes(
        IEnumerable<InspectedType> applicationTypes,
        IEnumerable<InspectedType>? universe = null)
    {
        var diagnostics = new List<Diagnostic>();
        var materialized = applicationTypes.Where(x => x.IsPublic && !x.IsAbstract).ToArray();
        var all = universe?.ToArray() ?? materialized;
        foreach (var type in materialized)
        {
            var kind = Classify(type, all);
            if (kind is "provider" or "repository")
            {
                var convention = type.Interfaces.Count(x =>
                    SimpleName(x).Equals("I" + StripGeneric(type.Name), StringComparison.Ordinal));
                if (convention != 1)
                    diagnostics.Add(new("PE3001", "error",
                        $"{type.FullName} must implement exactly one convention interface named I{StripGeneric(type.Name)}; found {convention}.",
                        type.AssemblyName));
            }

            if (kind == "controller" && HasAnonymousControllerMetadata(type, all) &&
                !HasIndependentAuthorization(type, all))
            {
                diagnostics.Add(new("PE4001", "warning",
                    $"{type.FullName} inherits anonymous controller metadata without an independent authorization filter on its type hierarchy or every inherited action.",
                    type.AssemblyName));
            }
        }

        foreach (var capability in materialized
                     .SelectMany(type => IdentifierCandidates(type, all)
                         .Select(id => (Capability: CapabilityName(type.Name), Type: type, Id: id)))
                     .GroupBy(x => x.Capability, StringComparer.OrdinalIgnoreCase))
        {
            var ids = capability.Select(x => x.Id).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (ids.Length > 1)
                diagnostics.Add(new("PE3002", "error",
                    $"Capability '{capability.Key}' uses inconsistent identifier types: {string.Join(", ", ids)}.",
                    string.Join(", ", capability.Select(x => x.Type.AssemblyName).Distinct().Order())));
        }

        return diagnostics.OrderBy(x => x.Code).ThenBy(x => x.Message, StringComparer.Ordinal).ToArray();
    }

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
            catch (Exception exception) when (exception is IOException or System.Xml.XmlException)
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
                    diagnostics.Add(new("PE2001", "error",
                        $"{Path.GetFileNameWithoutExtension(project)} ({source.Value.Name}) references {targetName} ({target.Value.Name}) against the canonical dependency direction.",
                        project));
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
        if (Ancestry(type, universe ?? [type]).Any(x =>
                DefinitionName(x.FullName).Equals("Microsoft.AspNetCore.Mvc.ControllerBase", StringComparison.Ordinal) ||
                DefinitionName(x.FullName).StartsWith("Paradigm.Enterprise.WebApi.Controllers.", StringComparison.Ordinal)))
            return "controller";
        if (type.Interfaces.Any(x => ContainsType(x, "Paradigm.Enterprise.Interfaces.IEntity")))
            return type.Name.EndsWith("View", StringComparison.Ordinal) ? "view" : "entity";
        if (type.Name.EndsWith("View", StringComparison.Ordinal))
            return "view";
        return null;
    }

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

    private static IEnumerable<string> IdentifierCandidates(InspectedType type, IReadOnlyList<InspectedType> all)
    {
        foreach (var current in Ancestry(type, all))
        foreach (var contract in current.Interfaces.Append(current.BaseType ?? ""))
        {
            var definition = DefinitionName(contract);
            var simpleDefinition = SimpleName(definition);
            var args = GenericArguments(contract);
            if (args.Count == 0)
                continue;

            if (simpleDefinition is "IEntity" or "EntityBase")
            {
                yield return args[0];
                continue;
            }

            if (simpleDefinition is "IReadRepository" or "IEditRepository" or
                "IReadProvider" or "IEditProvider" or
                "ReadApiControllerBase" or "EditApiControllerBase")
                yield return args[^1];
        }
    }

    private static IReadOnlyList<string> GenericArguments(string value)
    {
        var start = value.IndexOf('<');
        var end = value.LastIndexOf('>');
        if (start < 0 || end <= start)
            return [];
        var result = new List<string>();
        var depth = 0;
        var segment = start + 1;
        for (var i = start + 1; i < end; i++)
        {
            if (value[i] == '<') depth++;
            if (value[i] == '>') depth--;
            if (value[i] == ',' && depth == 0)
            {
                result.Add(value[segment..i].Trim());
                segment = i + 1;
            }
        }
        result.Add(value[segment..end].Trim());
        return result;
    }

    private static bool HasAnonymousControllerMetadata(InspectedType type, IReadOnlyList<InspectedType> all) =>
        Ancestry(type, all).Any(x =>
            x.Attributes.Any(attribute => attribute.EndsWith(".AllowAnonymousAttribute", StringComparison.Ordinal)) ||
            DefinitionName(x.FullName).StartsWith("Paradigm.Enterprise.WebApi.Controllers.", StringComparison.Ordinal));

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
            var next = all.FirstOrDefault(candidate =>
                DefinitionName(candidate.FullName).Equals(baseDefinition, StringComparison.Ordinal));
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

    private static bool IsIndependentAuthorization(string attribute) =>
        attribute is "Paradigm.Enterprise.WebApi.Attributes.ApiAuthorizationAttribute";

    private static bool ContainsType(string value, string name) =>
        value.Equals(name, StringComparison.Ordinal) || value.StartsWith(name + "<", StringComparison.Ordinal);

    private static string DefinitionName(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }

    private static string SimpleName(string value)
    {
        var name = DefinitionName(value).Split('.').Last();
        return StripGeneric(name);
    }

    private static string StripGeneric(string value)
    {
        var index = value.IndexOfAny(['`', '<']);
        return index < 0 ? value : value[..index];
    }

    private static string CapabilityName(string value)
    {
        var name = StripGeneric(value);
        foreach (var suffix in new[] { "ViewRepository", "Repository", "ReadProvider", "EditProvider", "Provider", "Controller", "Entity", "View" })
            if (name.EndsWith(suffix, StringComparison.Ordinal))
                return name[..^suffix.Length];
        return name;
    }

    private static (int Rank, string Name)? Layer(string projectName)
    {
        if (projectName.EndsWith(".Interfaces", StringComparison.OrdinalIgnoreCase)) return (0, "Interfaces");
        if (projectName.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase)) return (1, "Domain");
        if (projectName.EndsWith(".Data", StringComparison.OrdinalIgnoreCase) ||
            projectName.Contains(".Data.", StringComparison.OrdinalIgnoreCase)) return (2, "Data");
        if (projectName.EndsWith(".Providers", StringComparison.OrdinalIgnoreCase)) return (3, "Providers");
        if (projectName.EndsWith(".WebApi", StringComparison.OrdinalIgnoreCase) ||
            projectName.EndsWith(".Api", StringComparison.OrdinalIgnoreCase) ||
            projectName.EndsWith(".Host", StringComparison.OrdinalIgnoreCase)) return (4, "WebApi/Host");
        return null;
    }
}
