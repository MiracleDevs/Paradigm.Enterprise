namespace Paradigm.Enterprise.Domain.Mappers;

/// <summary>
/// Identifies a service as a domain object mapper.
/// </summary>
public interface IMapper
{
}

/// <summary>
/// Maps values in both directions between two object models.
/// </summary>
/// <typeparam name="TFrom">The first model type.</typeparam>
/// <typeparam name="TTo">The second model type.</typeparam>
public interface IMapper<TFrom, TTo> : IMapper
{
    /// <summary>
    /// Copies values from the first model into an existing second-model instance.
    /// </summary>
    /// <param name="source">The first-model value to read.</param>
    /// <param name="destination">The second-model instance to update.</param>
    /// <returns>The updated <paramref name="destination"/> instance.</returns>
    TTo MapTo(TFrom source, TTo destination);

    /// <summary>
    /// Copies values from the second model into an existing first-model instance.
    /// </summary>
    /// <param name="destination">The first-model instance to update.</param>
    /// <param name="source">The second-model value to read.</param>
    /// <returns>The updated <paramref name="destination"/> instance.</returns>
    TFrom MapFrom(TFrom destination, TTo source);
}
