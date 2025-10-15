using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json.Nodes;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonTableSchema : TableSchemaBase
{
    #region Properties

    /// <summary>
    /// The json object
    /// </summary>
    private readonly JsonObject _jsonObject;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTableSchema"/> class.
    /// </summary>
    /// <param name="jsonObject">The json object.</param>
    public JsonTableSchema(JsonObject jsonObject)
    {
        _jsonObject = jsonObject;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected override IList<IColumn> GetTableSchemaColumns()
    {
        var rootPropertyName = _jsonObject.First().Key;
        var firstItem = _jsonObject[rootPropertyName]?.AsArray().First()?.AsObject();

        if (firstItem is null)
            return new List<IColumn>();

        GenerateColumnsNames(firstItem.Count);

        return firstItem
            .Select((x, i) =>
            {
                var columnName = SourceHasHeader ? x.Key : GetGeneratedColumnName(i);
                return new JsonColumn(i, columnName, typeof(string));
            })
            .Cast<IColumn>()
            .ToList();
    }

    #endregion
}