namespace Paradigm.Enterprise.Providers;

/// <summary>
/// Identifies an application-layer provider that can be resolved from dependency injection.
/// </summary>
/// <remarks>
/// Use provider interfaces as application boundaries for controllers, background jobs, and other
/// consumers. Register concrete providers with the lifetime of the repositories and unit of work
/// they coordinate, normally scoped.
/// </remarks>
public interface IProvider
{
}
