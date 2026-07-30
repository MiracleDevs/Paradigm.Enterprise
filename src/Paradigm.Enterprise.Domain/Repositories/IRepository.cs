namespace Paradigm.Enterprise.Domain.Repositories;

/// <summary>
/// Marks a disposable persistence repository that participates in dependency-injected data access.
/// </summary>
public interface IRepository : IDisposable
{
}
