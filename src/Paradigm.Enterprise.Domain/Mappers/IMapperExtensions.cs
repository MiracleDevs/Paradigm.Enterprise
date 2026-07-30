namespace Paradigm.Enterprise.Domain.Mappers;

/// <summary>
/// Creates destination instances and maps individual values or sequences through an <see cref="IMapper{TFrom,TTo}"/>.
/// </summary>
public static class IMapperExtensions
{
    /// <summary>
    /// Maps a sequence of first-model values into newly constructed second-model instances.
    /// </summary>
    /// <typeparam name="TFrom">The source model type.</typeparam>
    /// <typeparam name="TTo">The destination model type.</typeparam>
    /// <param name="mapper">The mapper to invoke for each item.</param>
    /// <param name="from">The source sequence.</param>
    /// <returns>A materialized list of mapped values in source order.</returns>
    public static List<TTo> MapTo<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TFrom> from) where TTo : new()
    {
        return from.Select(x => mapper.MapTo(x, new TTo())).ToList();
    }

    /// <summary>
    /// Maps a sequence of second-model values into newly constructed first-model instances.
    /// </summary>
    /// <typeparam name="TFrom">The destination model type.</typeparam>
    /// <typeparam name="TTo">The source model type.</typeparam>
    /// <param name="mapper">The mapper to invoke for each item.</param>
    /// <param name="to">The source sequence.</param>
    /// <returns>A materialized list of mapped values in source order.</returns>
    public static List<TFrom> MapFrom<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TTo> to) where TFrom : new()
    {
        return to.Select(x => mapper.MapFrom(new TFrom(), x)).ToList();
    }

    /// <summary>
    /// Maps one first-model value into a newly constructed second-model instance.
    /// </summary>
    /// <typeparam name="TFrom">The source model type.</typeparam>
    /// <typeparam name="TTo">The destination model type.</typeparam>
    /// <param name="mapper">The mapper to invoke.</param>
    /// <param name="from">The source value.</param>
    /// <returns>The newly constructed and mapped destination.</returns>
    public static TTo MapTo<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, TFrom from) where TTo : new()
    {
        return mapper.MapTo(from, new TTo());
    }

    /// <summary>
    /// Maps one second-model value into a newly constructed first-model instance.
    /// </summary>
    /// <typeparam name="TFrom">The destination model type.</typeparam>
    /// <typeparam name="TTo">The source model type.</typeparam>
    /// <param name="mapper">The mapper to invoke.</param>
    /// <param name="to">The source value.</param>
    /// <returns>The newly constructed and mapped destination.</returns>
    public static TFrom MapFrom<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, TTo to) where TFrom : new()
    {
        return mapper.MapFrom(new TFrom(), to);
    }
}
