namespace Paradigm.Enterprise.Services.Core;

/// <summary>
/// Marks an application service for convention-based discovery and registration.
/// </summary>
/// <remarks>
/// The WebApi convention scanner registers concrete service types as singletons and registers an
/// implemented interface when its name matches <c>I{ImplementationName}</c>. Those are separate
/// implementation-type registrations: resolving the concrete type and matching interface can create
/// two distinct singleton instances. Stateful services must not assume both service keys are aliases
/// for the same object. Discovered implementations must also have a constructor that dependency
/// injection can activate; the scanner does not validate constructibility.
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
/// <c>ClockService</c> and <c>IClockService</c>, but consumers should consistently resolve one service
/// key when reference identity matters.
/// </example>
public interface IService
{
}
