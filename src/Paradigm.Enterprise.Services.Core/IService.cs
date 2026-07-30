namespace Paradigm.Enterprise.Services.Core;

/// <summary>
/// Marks an application service for convention-based discovery and registration.
/// </summary>
/// <remarks>
/// The WebApi convention scanner registers concrete service types as singletons and registers an
/// implemented interface when its name matches <c>I{ImplementationName}</c>.
/// </remarks>
/// <example>
/// <code>
/// public interface IClockService : IService
/// {
///     DateTimeOffset GetUtcNow();
/// }
///
/// public sealed class ClockService : IClockService
/// {
///     public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
/// }
/// </code>
/// Passing the containing assembly to <c>RegisterServices</c> registers both
/// <c>ClockService</c> and <c>IClockService</c>.
/// </example>
public interface IService
{
}
