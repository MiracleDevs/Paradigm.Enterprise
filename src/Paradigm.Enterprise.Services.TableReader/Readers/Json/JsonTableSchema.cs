using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonTableSchema : TableSchemaBase
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
    /// Initializes a new instance of the <see cref="JsonTableSchema"/> class.
    /// </summary>
    /// <param name="items">The json items.</param>
    public JsonTableSchema(IReadOnlyList<JsonElement> items)
    {
        Items = items;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected override IList<IColumn> GetTableSchemaColumns()
    {
        if (Items.Count == 0)
            return [];

        var firstItem = Items[0];

        if (firstItem.ValueKind != JsonValueKind.Object)
            throw new FormatException("Each json item must be an object.");

        var properties = firstItem.EnumerateObject().ToList();

        GenerateColumnsNames(properties.Count);

        return properties
            .Select((property, i) =>
            {
                var columnName = SourceHasHeader ? property.Name : GetGeneratedColumnName(i);
                return new JsonColumn(i, columnName, typeof(string));
            })
            .Cast<IColumn>()
            .ToList();
    }

    #endregion
}
