namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Specifies whether a dependency is managed locally or supplied externally.
/// </summary>
public enum ParadigmResourceMode
{
    /// <summary>
    /// Aspire creates and manages the local dependency.
    /// </summary>
    Managed,

    /// <summary>
    /// The dependency is supplied through a connection string.
    /// </summary>
    External
}
