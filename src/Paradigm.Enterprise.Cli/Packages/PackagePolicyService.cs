using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class PackagePolicyService
{
    #region Public Methods

    public IReadOnlyList<Diagnostic> Check(IReadOnlyList<PackageInfo> paradigmPackages, IReadOnlyList<RestoredPackage> restored, ParadigmConfiguration configuration)
    {
        var diagnostics = ResponseFactory.VersionDiagnostics(paradigmPackages).ToList();
        diagnostics.AddRange(SuppressionPolicy.Expired(configuration.Suppressions, configuration.Path));
        return diagnostics.Where(x => !SuppressionPolicy.IsSuppressed(x, configuration.Suppressions)).OrderBy(x => x.Code).ThenBy(x => x.Location, StringComparer.Ordinal).ToArray();
    }

    #endregion

    #region Private Methods

    internal static int CompareVersions(string left, string right) => SemanticVersion.Parse(left).CompareTo(SemanticVersion.Parse(right));

    #endregion
}