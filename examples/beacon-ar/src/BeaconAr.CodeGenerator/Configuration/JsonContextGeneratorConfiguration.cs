namespace BeaconAr.CodeGenerator.Configuration;

internal class JsonContextGeneratorConfiguration
{
    /// <summary>
    /// Gets the output path.
    /// </summary>
    /// <value>
    /// The output path.
    /// </value>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets or sets the ignored types.
    /// </summary>
    /// <value>
    /// The ignored types.
    /// </value>
    public string[]? IgnoredTypes { get; init; }
}