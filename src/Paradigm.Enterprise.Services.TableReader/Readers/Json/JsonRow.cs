using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json.Nodes;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonRow : RowBase
{
    #region Properties

    /// <summary>
    /// The json object
    /// </summary>
    private readonly JsonObject _jsonObject;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRow"/> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    public JsonRow(ITableSchema tableSchema, JsonObject jsonObject) : base(tableSchema)
    {
        _jsonObject = jsonObject;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
        var rootPropertyName = _jsonObject.First().Key;
        var items = _jsonObject[rootPropertyName]?.AsArray().ToList();

        if (items is null || Index >= items.Count)
            return false;

        Values = items[Index]?.AsObject()
            .Select(x => x.Value?.ToString() ?? string.Empty)
            .ToList() ?? new List<string>();

        Index++;

        if (Values.Count == 0 || Values.Count == 1 && string.IsNullOrEmpty(Values[0]))
            return false;

        ValidateValuesSchema();

        return true;
    }

    #endregion
}