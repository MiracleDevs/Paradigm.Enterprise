using Paradigm.Enterprise.Data.StoredProcedures.Mappers;
using System.Collections;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.Extensions;

/// <summary>
/// Materializes primitives, mapped objects, and lists from the current result set of a data reader.
/// </summary>
/// <remarks>
/// Object types require a mapper registered with <see cref="DataReaderMapperFactory"/>.
/// These methods consume rows but do not dispose or close the reader.
/// </remarks>
public static class DbDataReaderExtensions
{
    #region Public Methods

    /// <summary>
    /// Materializes the current result set as a primitive, mapped object, or list.
    /// </summary>
    /// <typeparam name="T">The result type to materialize.</typeparam>
    /// <param name="reader">The reader positioned before the first row of the current result set.</param>
    /// <returns>The materialized value, or the default value when a scalar or object result has no row.</returns>
    public static async Task<T?> TranslateAsync<T>(this DbDataReader reader)
    {
        var resultType = typeof(T);

        if (resultType.IsPrimitive)
            return await TranslatePrimitiveAsync<T>(reader);

        if (typeof(IList).IsAssignableFrom(resultType))
            return await TranslateListAsync<T>(reader, resultType);

        return await TranslateSingleAsync<T>(reader, resultType);
    }

    /// <summary>
    /// Materializes the current result set and then advances to the next result set.
    /// </summary>
    /// <typeparam name="T">The result type to materialize.</typeparam>
    /// <param name="reader">The reader positioned before the first row of the current result set.</param>
    /// <returns>The materialized value, or the default value when a scalar or object result has no row.</returns>
    public static async Task<T?> TranslateAndMoveAsync<T>(this DbDataReader reader)
    {
        var result = await reader.TranslateAsync<T>();
        await reader.NextResultAsync();
        return result;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Translates a single entity.
    /// </summary>
    /// <typeparam name="T">The object type to materialize.</typeparam>
    /// <param name="reader">The reader.</param>
    /// <param name="resultType">Type of the result.</param>
    /// <returns>The mapped first row, or the default value when the result set is empty.</returns>
    private static async Task<T?> TranslateSingleAsync<T>(DbDataReader reader, Type resultType)
    {
        var objectMapper = DataReaderMapperFactory.GetMapper<T>();
        if (await reader.ReadAsync())
            return (T)objectMapper.Map(reader);

        return default;
    }

    /// <summary>
    /// Translates a list of entities.
    /// </summary>
    /// <typeparam name="T">The concrete list type to create.</typeparam>
    /// <param name="reader">The reader.</param>
    /// <param name="resultType">Type of the result.</param>
    /// <returns>The list containing all mapped rows.</returns>
    /// <exception cref="Exception">The requested list type cannot be instantiated.</exception>
    private static async Task<T> TranslateListAsync<T>(DbDataReader reader, Type resultType)
    {
        var listItemType = resultType.GetGenericArguments().First();
        var objectMapper = DataReaderMapperFactory.GetMapper(listItemType);

        if (Activator.CreateInstance(resultType) is not IList results)
            throw new Exception($"Couldn't instantiate the type {resultType.Name}");

        while (await reader.ReadAsync())
            results.Add(objectMapper.Map(reader));

        return (T)results;
    }

    /// <summary>
    /// Translates a primitive.
    /// </summary>
    /// <typeparam name="T">The primitive type to read.</typeparam>
    /// <param name="reader">The reader.</param>
    /// <returns>The first column of the first row, or the default value when the result set is empty.</returns>
    private static async Task<T?> TranslatePrimitiveAsync<T>(DbDataReader reader)
    {
        if (await reader.ReadAsync())
            return (T)reader.GetValue(0);

        return default;
    }

    #endregion
}
