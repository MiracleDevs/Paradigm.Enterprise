using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonRow : RowBase
{
    #region Properties

    /// <summary>
    /// Gets the json items.
    /// </summary>
    /// <value>
    /// The json items.
    /// </value>
    private IReadOnlyList<JsonElement> Items { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRow"/> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    /// <param name="items">The json items.</param>
    public JsonRow(ITableSchema tableSchema, IReadOnlyList<JsonElement> items) : base(tableSchema)
    {
        Items = items;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
        if (Index >= Items.Count)
            return false;

        var item = Items[Index];

        if (item.ValueKind != JsonValueKind.Object)
            throw new FormatException("Each json item must be an object.");

        // Build a dictionary of JSON properties by name (case-insensitive) for quick lookup
        var propertyDictionary = item
            .EnumerateObject()
            .ToDictionary(
                x => x.Name,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase);

        // Populate Values in schema column order
        Values = TableSchema
            .GetColumns()
            .Select(column => GetColumnValue(propertyDictionary, column))
            .ToList();

        Index++;

        if (Values.Count == 0 || (Values.Count == 1 && string.IsNullOrEmpty(Values[0])))
            return false;

        ValidateValuesSchema();

        return true;
    }

    /// <summary>
    /// Converts a JSON value to its scalar string representation.
    /// </summary>
    /// <param name="value">The JSON value.</param>
    /// <returns>The scalar string representation.</returns>
    private static string GetValueAsString(JsonElement value)
    {
        return value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? string.Empty
            : value.ToString();
    }

    /// <summary>
    /// Gets the column value from the property dictionary.
    /// </summary>
    /// <param name="propertyDictionary">The dictionary of JSON properties by name.</param>
    /// <param name="columnName">The name of the column to retrieve.</param>
    /// <returns>The scalar string representation of the column value, or empty string if the property is missing.</returns>
    private static string GetColumnValue(Dictionary<string, JsonElement> propertyDictionary, string columnName)
    {
        if (propertyDictionary.TryGetValue(columnName, out var value))
            return GetValueAsString(value);

        return string.Empty;
    }

    /// <summary>
    /// Gets the column value from the property dictionary.
    /// </summary>
    /// <param name="propertyDictionary">The dictionary of JSON properties by name.</param>
    /// <param name="column">The schema column.</param>
    /// <returns>The scalar string representation of the column value, or empty string if the property is missing.</returns>
    private static string GetColumnValue(Dictionary<string, JsonElement> propertyDictionary, IColumn column)
    {
        var propertyName = column is JsonColumn jsonColumn ? jsonColumn.PropertyName : column.Name;
        return GetColumnValue(propertyDictionary, propertyName);
    }

    #endregion
}
