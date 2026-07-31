namespace Paradigm.Enterprise.Cli;

internal static class ResponseFactory
{
    #region Constants

    public const string SchemaVersion = "1.1";

    #endregion

    #region Public Methods

    public static CommandResponse Create(string command, IEnumerable<PackageInfo> packages, IEnumerable<ResultItem> results, IEnumerable<Diagnostic> diagnostics, GuideInfo? guide = null, CheckData? checks = null)
    {
        var orderedDiagnostics = diagnostics.DistinctBy(x => (x.Code, x.Severity, x.Message, x.Location)).OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Message, StringComparer.Ordinal).ThenBy(x => x.Location, StringComparer.Ordinal).ToArray();
        var status = orderedDiagnostics.Any(x => x.Severity == "error") ? "error" : orderedDiagnostics.Length > 0 ? "warning" : "success";
        return new(SchemaVersion, command, status, packages.GroupBy(x => (x.Name, x.Version)).Select(group =>
        {
            var projects = group.Select(x => x.Project).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal).ToArray();
            return new PackageInfo(group.Key.Name, group.Key.Version, projects.Length == 1 ? projects[0] : $"{projects.Length} projects");
        }).OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Version, StringComparer.Ordinal).ToArray(), results.OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.Name, StringComparer.Ordinal).ToArray(), orderedDiagnostics, guide, checks);
    }

    public static IReadOnlyList<Diagnostic> VersionDiagnostics(IEnumerable<PackageInfo> packages)
    {
        var versions = packages.Select(x => x.Version).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        var precedenceVersions = versions.Select(version => SemanticVersion.TryParse(version, out var semantic) ? semantic : null).ToArray();
        var mixed = precedenceVersions.All(x => x is not null) ? precedenceVersions.Distinct().Count() > 1 : versions.Length > 1;
        return !mixed ? [] : [new("PE1001", "error", $"Paradigm packages use mixed versions: {string.Join(", ", versions)}.")];
    }

    #endregion
}