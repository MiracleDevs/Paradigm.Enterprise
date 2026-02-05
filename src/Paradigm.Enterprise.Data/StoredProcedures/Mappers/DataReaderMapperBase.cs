using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

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
    /// Maps the specified reader.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns></returns>
    public abstract object Map(IDataReader reader);

    #endregion

    #region Protected Methods

    /// <summary>
    /// Loads the reader fields.
    /// </summary>
    /// <param name="reader">The reader.</param>
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
    /// Checks if the fields the is valid to map.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
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
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual string GetString(IDataReader reader, string name) => reader.GetString(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the character.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual char GetChar(IDataReader reader, string name) => reader.GetChar(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int16.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual short GetInt16(IDataReader reader, string name) => reader.GetInt16(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int32.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual int GetInt32(IDataReader reader, string name) => reader.GetInt32(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the int64.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual long GetInt64(IDataReader reader, string name) => reader.GetInt64(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the double.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual double GetDouble(IDataReader reader, string name) => reader.GetDouble(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the decimal.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual decimal GetDecimal(IDataReader reader, string name) => reader.GetDecimal(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the float.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual float GetFloat(IDataReader reader, string name) => reader.GetFloat(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the boolean.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual bool GetBoolean(IDataReader reader, string name) => (bool)reader.GetValue(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the unique identifier.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual Guid GetGuid(IDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the date time.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual DateTime GetDateTime(IDataReader reader, string name) => reader.GetDateTime(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the date time offset.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual DateTimeOffset GetDateTimeOffset(IDataReader reader, string name) => (DateTimeOffset)reader.GetValue(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the byte.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual byte GetByte(IDataReader reader, string name) => reader.GetByte(reader.GetOrdinal(name));

    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="name">The name.</param>
    /// <returns></returns>
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
    /// <returns></returns>
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
    /// <returns></returns>
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