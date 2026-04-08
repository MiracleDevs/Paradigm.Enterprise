using System;

namespace Paradigm.Enterprise.Interfaces;

public interface IEntity
{
    bool IsNew();
}

public interface IEntity<TId>
    : IEntity
    where TId : struct, IEquatable<TId>
{
    TId Id { get; }
}