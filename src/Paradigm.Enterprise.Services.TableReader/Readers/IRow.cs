namespace Paradigm.Enterprise.Services.TableReader.Readers;

/// <summary>
/// Provides indexed and typed access to the current table record.
/// </summary>
/// <remarks>
/// Column positions are zero-based. <see cref="Index"/> is zero before the first read and becomes
/// the one-based ordinal of the most recently processed data row. Typed accessors convert the raw
/// scalar using <see cref="Convert"/>. CSV numeric, date, and Boolean conversions use
/// <see cref="Paradigm.Enterprise.Services.TableReader.Configuration.CsvParserConfiguration.Culture"/> or invariant culture when it is
/// unset; other formats use the standard <see cref="Convert"/> behavior. Empty text is not treated
/// as null. Invalid positions and failed conversions propagate their corresponding
/// <see cref="IndexOutOfRangeException"/>, <see cref="FormatException"/>,
/// <see cref="InvalidCastException"/>, or <see cref="OverflowException"/>.
/// </remarks>
public interface IRow
{
    /// <summary>
    /// Gets the one-based ordinal of the most recently processed data row.
    /// </summary>
    /// <value>
    /// Zero before the first read; otherwise the one-based data-row ordinal.
    /// </value>
    int Index { get; }

    /// <summary>
    /// Gets the value at the specified zero-based column index.
    /// </summary>
    /// <value>
    /// The raw scalar value.
    /// </value>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The raw value, or <see langword="null"/> when no value is present.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the current row.</exception>
    object? this[int index] { get; }

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when a non-empty row was read and validated; <see langword="false"/>
    /// at end of input or when the processed row is empty.
    /// </returns>
    /// <exception cref="FormatException">The source row does not conform to its format's expected shape.</exception>
    /// <exception cref="Paradigm.Enterprise.Services.TableReader.Readers.Base.TableSchemaException">The row contains more values than the initialized schema.</exception>
    bool Read();

    /// <summary>
    /// Converts the scalar at a zero-based column position to a Boolean.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The Boolean represented by the scalar.</returns>
    bool GetBoolean(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to an unsigned 8-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The unsigned 8-bit integer represented by the scalar.</returns>
    byte GetByte(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a character.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The character represented by the scalar.</returns>
    char GetChar(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a date and time.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The date and time represented by the scalar.</returns>
    DateTime GetDateTime(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a decimal number.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The decimal number represented by the scalar.</returns>
    decimal GetDecimal(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a double-precision number.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The double-precision number represented by the scalar.</returns>
    double GetDouble(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a single-precision number.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The single-precision number represented by the scalar.</returns>
    float GetSingle(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a signed 16-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The signed 16-bit integer represented by the scalar.</returns>
    short GetInt16(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a signed 32-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The signed 32-bit integer represented by the scalar.</returns>
    int GetInt32(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a signed 64-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The signed 64-bit integer represented by the scalar.</returns>
    long GetInt64(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to a signed 8-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The signed 8-bit integer represented by the scalar.</returns>
    sbyte GetSByte(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to text.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The scalar's text representation, or <see langword="null"/> for a null scalar.</returns>
    string? GetString(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to an unsigned 16-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The unsigned 16-bit integer represented by the scalar.</returns>
    ushort GetUInt16(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to an unsigned 32-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The unsigned 32-bit integer represented by the scalar.</returns>
    uint GetUInt32(int index);

    /// <summary>
    /// Converts the scalar at a zero-based column position to an unsigned 64-bit integer.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The unsigned 64-bit integer represented by the scalar.</returns>
    ulong GetUInt64(int index);

    /// <summary>
    /// Gets the raw scalar at a zero-based column position.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>The raw scalar, or <see langword="null"/> when the implementation exposes a null value.</returns>
    object? GetValue(int index);

    /// <summary>
    /// Determines whether the scalar at a zero-based column position is null or <see cref="DBNull"/>.
    /// </summary>
    /// <param name="index">The zero-based column position.</param>
    /// <returns>
    /// <see langword="true"/> for a null or <see cref="DBNull"/> scalar; otherwise
    /// <see langword="false"/>. An empty string returns <see langword="false"/>.
    /// </returns>
    bool IsNull(int index);

    /// <summary>
    /// Gets the raw scalar identified by a schema column.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The raw scalar, or <see langword="null"/> when the implementation exposes a null value.</returns>
    object? GetValue(IColumn column);

    /// <summary>
    /// Determines whether the value specified by the column is null.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>
    /// <see langword="true"/> for a null or <see cref="DBNull"/> scalar; otherwise
    /// <see langword="false"/>. An empty string returns <see langword="false"/>.
    /// </returns>
    bool IsNull(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to an unsigned 8-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The unsigned 8-bit integer represented by the scalar.</returns>
    byte GetByte(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a signed 8-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The signed 8-bit integer represented by the scalar.</returns>
    sbyte GetSByte(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to an unsigned 16-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The unsigned 16-bit integer represented by the scalar.</returns>
    ushort GetUInt16(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a signed 16-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The signed 16-bit integer represented by the scalar.</returns>
    short GetInt16(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to an unsigned 32-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The unsigned 32-bit integer represented by the scalar.</returns>
    uint GetUInt32(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a signed 32-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The signed 32-bit integer represented by the scalar.</returns>
    int GetInt32(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to an unsigned 64-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The unsigned 64-bit integer represented by the scalar.</returns>
    ulong GetUInt64(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a signed 64-bit integer.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The signed 64-bit integer represented by the scalar.</returns>
    long GetInt64(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a single-precision number.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The single-precision number represented by the scalar.</returns>
    float GetSingle(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a double-precision number.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The double-precision number represented by the scalar.</returns>
    double GetDouble(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a decimal number.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The decimal number represented by the scalar.</returns>
    decimal GetDecimal(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a character.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The character represented by the scalar.</returns>
    char GetChar(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to text.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The scalar's text representation, or <see langword="null"/> for a null scalar.</returns>
    string? GetString(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a date and time.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The date and time represented by the scalar.</returns>
    DateTime GetDateTime(IColumn column);

    /// <summary>
    /// Converts the scalar identified by a schema column to a Boolean.
    /// </summary>
    /// <param name="column">The column whose zero-based <see cref="IColumn.Index"/> selects the value.</param>
    /// <returns>The Boolean represented by the scalar.</returns>
    bool GetBoolean(IColumn column);
}
