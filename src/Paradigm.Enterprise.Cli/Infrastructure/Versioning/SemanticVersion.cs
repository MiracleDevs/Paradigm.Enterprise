using System.Numerics;

namespace Paradigm.Enterprise.Cli;

internal sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    #region Fields

    private readonly IReadOnlyList<string> prerelease;

    #endregion

    #region Properties

    public BigInteger Major { get; }
    public BigInteger Minor { get; }
    public BigInteger Patch { get; }
    public bool IsPrerelease => prerelease.Count > 0;

    #endregion

    #region Constructors

    private SemanticVersion(BigInteger major, BigInteger minor, BigInteger patch, IReadOnlyList<string> prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        this.prerelease = prerelease;
    }

    #endregion

    #region Public Methods

    public static SemanticVersion Parse(string value) => TryParse(value, out var version) ? version : throw new FormatException($"'{value}' is not a valid Semantic Version.");
    public static bool TryParse(string? value, out SemanticVersion version)
    {
        version = null!;
        if (string.IsNullOrWhiteSpace(value) || !value.Equals(value.Trim(), StringComparison.Ordinal))
            return false;
        var buildSeparator = value.IndexOf('+');
        var precedence = buildSeparator < 0 ? value : value[..buildSeparator];
        if (buildSeparator >= 0 && !ValidIdentifiers(value[(buildSeparator + 1)..], false))
            return false;
        var prereleaseSeparator = precedence.IndexOf('-');
        var core = prereleaseSeparator < 0 ? precedence : precedence[..prereleaseSeparator];
        var prereleaseText = prereleaseSeparator < 0 ? null : precedence[(prereleaseSeparator + 1)..];
        var coreParts = core.Split('.');
        if (coreParts.Length != 3 || !TryNumeric(coreParts[0], out var major) || !TryNumeric(coreParts[1], out var minor) || !TryNumeric(coreParts[2], out var patch) || prereleaseText is not null && !ValidIdentifiers(prereleaseText, true))
            return false;
        version = new(major, minor, patch, prereleaseText?.Split('.') ?? []);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
            return 1;
        var core = Major.CompareTo(other.Major);
        if (core == 0)
            core = Minor.CompareTo(other.Minor);
        if (core == 0)
            core = Patch.CompareTo(other.Patch);
        if (core != 0)
            return core;
        if (!IsPrerelease || !other.IsPrerelease)
            return IsPrerelease == other.IsPrerelease ? 0 : IsPrerelease ? -1 : 1;
        for (var index = 0; index < Math.Min(prerelease.Count, other.prerelease.Count); index++)
        {
            var left = prerelease[index];
            var right = other.prerelease[index];
            var leftNumeric = BigInteger.TryParse(left, out var leftNumber);
            var rightNumeric = BigInteger.TryParse(right, out var rightNumber);
            var comparison = leftNumeric && rightNumeric ? leftNumber.CompareTo(rightNumber) : leftNumeric != rightNumeric ? leftNumeric ? -1 : 1 : string.Compare(left, right, StringComparison.Ordinal);
            if (comparison != 0)
                return comparison;
        }

        return prerelease.Count.CompareTo(other.prerelease.Count);
    }

    public bool Equals(SemanticVersion? other) => CompareTo(other) == 0;

    #endregion

    #region Overrides

    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Major);
        hash.Add(Minor);
        hash.Add(Patch);
        foreach (var identifier in prerelease)
            hash.Add(identifier, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    #endregion

    #region Private Methods

    private static bool TryNumeric(string value, out BigInteger number)
    {
        number = default;
        return value.Length > 0 && (value.Length == 1 || value[0] != '0') && value.All(character => character is >= '0' and <= '9') && BigInteger.TryParse(value, out number);
    }

    private static bool ValidIdentifiers(string value, bool rejectNumericLeadingZeroes) => value.Split('.').All(identifier => identifier.Length > 0 && identifier.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '-') && (!rejectNumericLeadingZeroes || !identifier.All(character => character is >= '0' and <= '9') || identifier.Length == 1 || identifier[0] != '0'));

    #endregion
}