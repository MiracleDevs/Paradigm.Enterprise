namespace Paradigm.Enterprise.Domain.Mappers;

public static class IMapperExtensions
{
    public static List<TTo> MapTo<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TFrom> from) where TTo : new()
    {
        return from.Select(x => mapper.MapTo(x, new TTo())).ToList();
    }

    public static List<TFrom> MapFrom<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, IEnumerable<TTo> to) where TFrom : new()
    {
        return to.Select(x => mapper.MapFrom(new TFrom(), x)).ToList();
    }

    public static TTo MapTo<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, TFrom from) where TTo : new()
    {
        return mapper.MapTo(from, new TTo());
    }

    public static TFrom MapFrom<TFrom, TTo>(this IMapper<TFrom, TTo> mapper, TTo to) where TFrom : new()
    {
        return mapper.MapFrom(new TFrom(), to);
    }
}