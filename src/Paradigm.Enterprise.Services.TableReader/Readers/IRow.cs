namespace Paradigm.Enterprise.Services.TableReader.Readers;

public interface IRow
{
    /// <summary>
    /// Gets the row index.
    /// </summary>
    /// <value>
    /// The row index.
    /// </value>
    int Index { get; }

    /// <summary>
    /// Gets the <see cref="object"/> with the specified column name.
    /// </summary>
    /// <value>
    /// The <see cref="object"/>.
    /// </value>
    /// <param name="index">Index of the column.</param>
    /// <returns></returns>
    object? this[int index] { get; }

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    bool Read();

    /// <summary>
    /// Gets a boolean value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A boolean value.</returns>
    bool GetBoolean(int index);

    /// <summary>
    /// Gets a byte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A byte value.</returns>
    byte GetByte(int index);

    /// <summary>
    /// Gets a char value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A char value.</returns>
    char GetChar(int index);

    /// <summary>
    /// Gets a DateTime value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A DateTime value.</returns>
    DateTime GetDateTime(int index);

    /// <summary>
    /// Gets a decimal value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A decimal value.</returns>
    decimal GetDecimal(int index);

    /// <summary>
    /// Gets a double value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A double value.</returns>
    double GetDouble(int index);

    /// <summary>
    /// Gets a float value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A float value.</returns>
    float GetSingle(int index);

    /// <summary>
    /// Gets a short value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A short value.</returns>
    short GetInt16(int index);

    /// <summary>
    /// Gets a int value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A int value.</returns>
    int GetInt32(int index);

    /// <summary>
    /// Gets a long value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A long value.</returns>
    long GetInt64(int index);

    /// <summary>
    /// Gets a sbyte value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A sbyte value.</returns>
    sbyte GetSByte(int index);

    /// <summary>
    /// Gets a string value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A string value.</returns>
    string? GetString(int index);

    /// <summary>
    /// Gets a ushort value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A ushort value.</returns>
    ushort GetUInt16(int index);

    /// <summary>
    /// Gets a uint value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A uint value.</returns>
    uint GetUInt32(int index);

    /// <summary>
    /// Gets a ulong value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A ulong value.</returns>
    ulong GetUInt64(int index);

    /// <summary>
    /// Gets a object value from the specified column name.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>A object value.</returns>
    object? GetValue(int index);

    /// <summary>
    /// Determines whether the value specified by the column name is null.
    /// </summary>
    /// <param name="index">Index of the column.</param>
    /// <returns>
    ///   <c>true</c> if the value is null; otherwise, <c>false</c>.
    /// </returns>
    bool IsNull(int index);
}