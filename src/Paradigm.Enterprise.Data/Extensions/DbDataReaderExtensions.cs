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
/// Lists must be requested as a constructible, closed generic <see cref="IList"/> implementation with
/// exactly one element type, such as <c>List&lt;T&gt;</c>. Nongeneric collections such as
/// <see cref="ArrayList"/> are invalid for the built-in list path. Interface-only generic list types
/// are not handled by that path and instead require a registered object mapper; prefer a concrete
/// <c>List&lt;T&gt;</c>. A result type is treated as a scalar only when
/// <see cref="Type.IsPrimitive"/> is <see langword="true"/>; types such as <see cref="string"/>,
/// <see cref="decimal"/>, and <see cref="DateTime"/> require a registered mapper.
/// </remarks>
/// <example>
/// Register result mappers once, execute the command, and translate each result set in sequence:
/// <code>
/// DataReaderMapperFactory.RegisterMapper&lt;OrderSummary&gt;(
///     () =&gt; new OrderSummaryMapper());
/// DataReaderMapperFactory.RegisterMapper&lt;OrderLine&gt;(
///     () =&gt; new OrderLineMapper());
///
/// await using DbDataReader reader = await command.ExecuteReaderAsync();
/// OrderSummary? order =
///     await reader.TranslateAndMoveAsync&lt;OrderSummary&gt;();
/// List&lt;OrderLine&gt; lines =
///     await reader.TranslateAsync&lt;List&lt;OrderLine&gt;&gt;() ?? [];
/// </code>
/// <see cref="TranslateAndMoveAsync{T}"/> advances exactly once after materializing the current result.
/// The caller still owns and disposes both the command and reader.
/// </example>
public static class DbDataReaderExtensions
{
    #region Public Methods

    /// <summary>
    /// Materializes the current result set as a primitive, mapped object, or list.
    /// </summary>
    /// <typeparam name="T">The result type to materialize.</typeparam>
    /// <param name="reader">The reader positioned before the first row of the current result set.</param>
    /// <returns>
    /// The materialized value. An empty scalar or object result returns the default value; a supported
    /// concrete list request returns an empty list.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// A non-primitive result or list element type has no registered mapper.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// A primitive database value or mapper result cannot be cast to the requested type.
    /// </exception>
    /// <exception cref="Exception">
    /// A type selected for the built-in list path cannot be constructed as an <see cref="IList"/>.
    /// </exception>
    /// <remarks>
    /// Primitive and object requests consume at most the first row. List requests consume every row
    /// in the current result set. This method does not advance to the next result set.
    /// </remarks>
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
    /// <returns>
    /// The materialized value. An empty scalar or object result returns the default value; a supported
    /// concrete list request returns an empty list.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// A non-primitive result or list element type has no registered mapper.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// A primitive database value or mapper result cannot be cast to the requested type.
    /// </exception>
    /// <exception cref="Exception">
    /// A type selected for the built-in list path cannot be constructed as an <see cref="IList"/>.
    /// </exception>
    /// <remarks>
    /// The reader is advanced by calling <see cref="DbDataReader.NextResultAsync(CancellationToken)"/>
    /// even when the current result set is empty.
    /// </remarks>
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
