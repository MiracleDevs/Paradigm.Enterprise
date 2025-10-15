using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text;
using System.Text.Json.Nodes;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonTableReader : TableReaderBase
{
    #region Properties

    /// <summary>
    /// Gets the json object.
    /// </summary>
    /// <value>
    /// The json object.
    /// </value>
    private readonly JsonObject _jsonObject;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTableReader" /> class.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    private JsonTableReader(string content, bool sourceHasHeader)
    {
        _jsonObject = JsonNode.Parse(content)?.AsObject() ?? throw new ArgumentNullException(nameof(_jsonObject));
        Schema = new JsonTableSchema(_jsonObject).Initialize(sourceHasHeader);
        CurrentRow = new JsonRow(Schema, _jsonObject);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Opens a new table reader from the specified content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromContent(byte[] content, bool sourceHasHeader)
    {
        return new JsonTableReader(Encoding.UTF8.GetString(content), sourceHasHeader);
    }

    #endregion
}