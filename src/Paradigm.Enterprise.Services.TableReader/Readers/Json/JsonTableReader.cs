using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTableReader" /> class from a JsonObject.
    /// </summary>
    /// <param name="jsonObject">The json object.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    private JsonTableReader(JsonObject jsonObject, bool sourceHasHeader)
    {
        _jsonObject = jsonObject ?? throw new ArgumentNullException(nameof(jsonObject));
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

    /// <summary>
    /// Opens a new table reader from the specified stream (asynchronous).
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static async Task<ITableReader> OpenFromStreamAsync(Stream contentStream, bool sourceHasHeader)
    {
        // Use JsonDocument for better memory management than JsonNode.Parse
        var jsonDocument = await JsonDocument.ParseAsync(contentStream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        // Convert to JsonObject for compatibility with existing schema/row logic
        var jsonObject = JsonSerializer.Deserialize<JsonObject>(jsonDocument.RootElement.GetRawText());
        if (jsonObject == null)
            throw new ArgumentNullException(nameof(contentStream), "Failed to deserialize JSON content");

        return new JsonTableReader(jsonObject, sourceHasHeader);
    }

    /// <summary>
    /// Opens a new table reader from the specified stream (synchronous).
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
    {
        // For synchronous compatibility, use GetAwaiter().GetResult()
        return OpenFromStreamAsync(contentStream, sourceHasHeader).GetAwaiter().GetResult();
    }

    #endregion
}