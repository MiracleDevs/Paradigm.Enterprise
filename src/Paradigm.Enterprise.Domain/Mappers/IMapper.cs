namespace Paradigm.Enterprise.Domain.Mappers;

public interface IMapper
{
}

public interface IMapper<TFrom, TTo> : IMapper
{
    /// <summary>
    /// Maps to.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="destination">The destination.</param>
    /// <returns></returns>
    TTo MapTo(TFrom source, TTo destination);

    /// <summary>
    /// Maps from.
    /// </summary>
    /// <param name="destination">The destination.</param>
    /// <param name="source">The source.</param>
    /// <returns></returns>
    TFrom MapFrom(TFrom destination, TTo source);
}