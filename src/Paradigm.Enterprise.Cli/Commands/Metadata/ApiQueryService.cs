namespace Paradigm.Enterprise.Cli;
internal sealed class ApiQueryService : IApiQueryService
{
#region Public Methods
    public IReadOnlyList<ResultItem> Search(IEnumerable<InspectedType> types, string query, string? package, int limit) => PublicParadigmTypes(types, package).SelectMany(type =>
    {
        var items = new List<ResultItem>();
        if (type.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
            items.Add(TypeResult(type));
        items.AddRange((type.StructuredMembers ?? []).Where(member => member.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || member.Signature.Contains(query, StringComparison.OrdinalIgnoreCase)).Select(member => new ResultItem("member", $"{type.FullName}.{member.Name}", member.Signature, type.Package, type.Version, type.AssemblyName)));
        return items;
    }).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Detail, StringComparer.Ordinal).Take(limit).ToArray();
    public ApiMatch Show(IEnumerable<InspectedType> types, string symbol, string? package)
    {
        var publicTypes = PublicParadigmTypes(types, package).ToArray();
        var matches = publicTypes.Where(x => x.FullName.Equals(symbol, StringComparison.OrdinalIgnoreCase) || x.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase) || UngenericName(x.Name).Equals(symbol, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.FullName, StringComparer.Ordinal).ToArray();
        if (matches.Length > 0)
            return Match(symbol, matches.Select(TypeResult).ToArray(), matches);
        var memberMatches = publicTypes.SelectMany(type => (type.StructuredMembers ?? []).Where(member => member.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase)).Select(member => (Type: type, Result: new ResultItem("member", $"{type.FullName}.{member.Name}", member.Signature, type.Package, type.Version, type.AssemblyName)))).OrderBy(x => x.Result.Name, StringComparer.Ordinal).ToArray();
        return Match(symbol, memberMatches.Select(x => x.Result).ToArray(), memberMatches.Select(x => x.Type).DistinctBy(x => x.FullName).ToArray());
    }

#endregion
#region Private Methods
    private static ApiMatch Match(string symbol, IReadOnlyList<ResultItem> results, IReadOnlyList<InspectedType> types)
    {
        if (results.Count == 0)
            return new(results, types, [new("PE0001", "error", $"Symbol '{symbol}' was not found in the resolved Paradigm packages.")]);
        if (types.Count > 1)
            return new(results, types, [new("PE0003", "error", $"Symbol '{symbol}' is ambiguous. Use a fully qualified symbol or --package. Matches: {string.Join(", ", types.Select(x => x.FullName).Order())}.")]);
        return new(results, types, []);
    }

    internal static ResultItem TypeResult(InspectedType type)
    {
        var detail = new List<string>();
        if (type.BaseType is not null)
            detail.Add($"base: {type.BaseType}");
        if (type.Interfaces.Count > 0)
            detail.Add($"interfaces: {string.Join(", ", type.Interfaces)}");
        detail.AddRange((type.StructuredMembers ?? []).Select(x => x.Signature));
        return new("type", type.FullName, string.Join('\n', detail), type.Package, type.Version, type.AssemblyName);
    }

    private static IEnumerable<InspectedType> PublicParadigmTypes(IEnumerable<InspectedType> types, string? package) => types.Where(x => x.IsPublic).Where(x => x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.OrdinalIgnoreCase)).Where(x => package is null || (x.Package ?? x.AssemblyName).Contains(package, StringComparison.OrdinalIgnoreCase));
    private static string UngenericName(string value)
    {
        var index = value.IndexOf('<');
        return index < 0 ? value : value[..index];
    }
#endregion
}
