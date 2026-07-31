using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.Cli;

internal static class SuppressionPolicy
{
    #region Public Methods

    public static IReadOnlyList<Diagnostic> Expired(IEnumerable<ConfiguredSuppression> suppressions, string? location) => suppressions.Where(x => x.Expires < DateOnly.FromDateTime(DateTime.UtcNow)).Select(x => new Diagnostic("PE7004", "error", $"Suppression for {x.Code} expired on {x.Expires:yyyy-MM-dd}: {x.Reason}", location)).OrderBy(x => x.Message, StringComparer.Ordinal).ToArray();
    public static bool IsSuppressed(Diagnostic diagnostic, IEnumerable<ConfiguredSuppression> suppressions) => suppressions.Any(x => x.Expires >= DateOnly.FromDateTime(DateTime.UtcNow) && x.Code.Equals(diagnostic.Code, StringComparison.Ordinal) && (string.IsNullOrWhiteSpace(x.Location) || diagnostic.Location?.Contains(x.Location, StringComparison.OrdinalIgnoreCase) == true) && (string.IsNullOrWhiteSpace(x.Symbol) || diagnostic.Message.Contains(x.Symbol, StringComparison.Ordinal) || diagnostic.Location?.Contains(x.Symbol, StringComparison.Ordinal) == true));

    #endregion
}