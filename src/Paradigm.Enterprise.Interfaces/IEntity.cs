using System;

namespace Paradigm.Enterprise.Interfaces;

/// <summary>
/// Defines an object whose persistence state can be classified as new or existing.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Determines whether the entity has not yet been assigned a persistent identity.
    /// </summary>
    /// <returns><see langword="true"/> when the entity is new; otherwise, <see langword="false"/>.</returns>
    bool IsNew();
}

/// <summary>
/// Defines an entity with a strongly typed persistent identifier.
/// </summary>
/// <typeparam name="TId">The value type used for the entity identifier.</typeparam>
public interface IEntity<TId>
    : IEntity
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <value>The persistent identifier, or its default value when the entity is new.</value>
    TId Id { get; }
}
