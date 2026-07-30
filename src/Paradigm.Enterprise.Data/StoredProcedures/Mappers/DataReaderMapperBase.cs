using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

/// <summary>
/// Caches result-set field names and provides typed column readers for custom data-reader mappers.
/// </summary>
/// <remarks>
/// Call <see cref="LoadReaderFields"/> before <see cref="FieldIsValid"/>. Field metadata is cached
/// per mapper instance, so an instance should be used with result sets that share the same schema.
/// The cached-name lookup is case-insensitive, but schema-table confirmation compares the requested
/// name with <c>ColumnName</c> using case-sensitive ordinal equality.
/// </remarks>
/// <example>
/// Implement <see cref="Map"/> for one row and load the schema before testing optional columns:
/// <code>
/// public sealed class OrderSummaryMapper : DataReaderMapperBase
/// {
///     public override object Map(IDataReader reader)
///     {
///         LoadReaderFields(reader);
///
///         return new OrderSummary
///         {
///             Id = GetInt32(reader, "Id"),
///             Number = GetString(reader, "Number"),
///             CustomerName = FieldIsValid(reader, "CustomerName")
///                 ? GetString(reader, "CustomerName")
///                 : null
///         };
///     }
/// }
///
/// DataReaderMapperFactory.RegisterMapper&lt;OrderSummary&gt;(
///     () =&gt; new OrderSummaryMapper());
/// </code>
/// The stored-procedure translation helpers advance the reader before calling <see cref="Map"/>.
/// The mapper must not call <c>Read</c>, <c>NextResult</c>, close, or dispose the reader. Call
/// <see cref="FieldIsValid"/> before a typed getter when a column may be absent or database-null.
/// </example>
public abstract class DataReaderMapperBase : IDataReaderMapper
{
    #region Properties

    /// <summary>
    /// The fields
    /// </summary>
    private ImmutableHashSet<string> _fields = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Public Methods

    /// <summary>
    /// Maps the data reader's current row to an application object.
    /// </summary>
    /// <param name="reader">The reader positioned on the row to map.</param>
    /// <returns>The application object mapped from the current row.</returns>
    /// <remarks>
    /// Implementations should call <see cref="LoadReaderFields"/> before using
    /// <see cref="FieldIsValid"/>. Do not advance or dispose <paramref name="reader"/>.
    /// </remarks>
    public abstract object Map(IDataReader reader);

    #endregion

    #region Protected Methods

    /// <summary>
    /// Caches the names exposed by a reader schema for optional-field checks.
    /// </summary>
    /// <param name="reader">The reader whose current result-set schema is inspected.</param>
    /// <remarks>
    /// Loading occurs only while the cache is empty. Reusing one mapper instance with a different
    /// result shape therefore retains the first successfully loaded set of names. Schema-read failures
    /// are ignored and leave the cache empty or partially populated.
    /// </remarks>
    protected void LoadReaderFields(IDataReader reader)
    {
        if (_fields.Any() || reader.FieldCount <= 0)
            return;

        try
        {
            var builder = _fields.ToBuilder();
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;

            for (var i = 0; i < reader.FieldCount; i++)
                builder.Add(reader.GetName(i));

            _fields = builder.ToImmutable();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Determines whether a named field exists and contains a non-database-null value.
    /// </summary>
    /// <param name="reader">The reader positioned on the row to inspect.</param>
    /// <param name="name">The database column name.</param>
    /// <returns>
    /// <see langword="true"/> when the named field exists and is not database null; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool FieldIsValid(IDataReader reader, string name)
    {
        if (!_fields.Contains(name) || !ReaderContainsField(reader, name))
            return false;

        var ordinal = reader.GetOrdinal(name);
        return !reader.IsDBNull(ordinal);
    }

    /// <summary>
    /// Gets the string.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The string stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual string GetString(IDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the character.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The character stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual char GetChar(IDataReader reader, string name) => reader.GetChar(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int16.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The 16-bit integer stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual short GetInt16(IDataReader reader, string name) => reader.GetInt16(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int32.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The 32-bit integer stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual int GetInt32(IDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int64.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The 64-bit integer stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual long GetInt64(IDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the double.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The double-precision value stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual double GetDouble(IDataReader reader, string name) => reader.GetDouble(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the decimal.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The decimal value stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual decimal GetDecimal(IDataReader reader, string name) => reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the float.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The single-precision value stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual float GetFloat(IDataReader reader, string name) => reader.GetFloat(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the boolean.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The Boolean value stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool GetBoolean(IDataReader reader, string name) => (bool)reader.GetValue(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The GUID stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Guid GetGuid(IDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the date time.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The date and time stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual DateTime GetDateTime(IDataReader reader, string name) => reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the date time offset.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The date, time, and offset stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual DateTimeOffset GetDateTimeOffset(IDataReader reader, string name) => (DateTimeOffset)reader.GetValue(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the byte.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The byte stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual byte GetByte(IDataReader reader, string name) => reader.GetByte(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>A new array containing the binary value stored in the named field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual byte[] GetBytes(IDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        var buffer = new byte[reader.GetBytes(ordinal, 0, null, 0, 0)];
        reader.GetBytes(ordinal, 0, buffer, 0, buffer.Length);
        return buffer;
    }

    /// <summary>
    /// Gets the array.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns>The typed array stored in the field, or <see langword="null"/> when its runtime type differs.</returns>
    protected virtual T[]? GetArray<T>(IDataReader reader, string name)
    {
        var databaseValue = reader.GetValue(reader.GetOrdinal(name));

        if (databaseValue is not null && databaseValue is T[] items)
            return items;

        return null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Check if the reader contains the specified field.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when the schema contains the named field; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ReaderContainsField(IDataReader reader, string name)
    {
        var schemaTable = reader.GetSchemaTable();
        if (schemaTable is null) return false;

        foreach (DataRow row in schemaTable.Rows)
            if (name.Equals(row["ColumnName"]))
                return true;

        return false;
    }

    #endregion
}
