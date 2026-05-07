using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Text.Json;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonTableReader : TableReaderBase
{
    #region Properties

    /// <summary>
    /// Gets the json document.
    /// </summary>
    /// <value>
    /// The json document.
    /// </value>
    private JsonDocument JsonDocument { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonTableReader" /> class.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    private JsonTableReader(Stream stream, bool sourceHasHeader)
    {
        JsonDocument = JsonDocument.Parse(stream);
        var items = GetItems(JsonDocument.RootElement);

        Schema = new JsonTableSchema(items).Initialize(sourceHasHeader);
        CurrentRow = new JsonRow(Schema, items);
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
        return new JsonTableReader(new MemoryStream(content), sourceHasHeader);
    }

    /// <summary>
    /// Opens a new table reader from the specified stream.
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
    {
        return new JsonTableReader(contentStream, sourceHasHeader);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        JsonDocument.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Gets the JSON array items from the root object.
    /// </summary>
    /// <param name="rootElement">The root element.</param>
    /// <returns>A list with all row items.</returns>
    /// <exception cref="FormatException">Thrown when JSON does not follow the expected shape.</exception>
    private static IReadOnlyList<JsonElement> GetItems(JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Object)
            throw new FormatException("The json root element must be an object.");

        JsonProperty? rootProperty = null;
        var rootPropertyCount = 0;

        foreach (var property in rootElement.EnumerateObject())
        {
            rootPropertyCount++;
            rootProperty = property;

            if (rootPropertyCount > 1)
                break;
        }

        if (rootPropertyCount != 1 || rootProperty is null)
            throw new FormatException("The json root object must contain a single property with an array value.");

        if (rootProperty.Value.Value.ValueKind != JsonValueKind.Array)
            throw new FormatException("The json root property value must be an array.");

        return rootProperty.Value.Value.EnumerateArray().ToList();
    }

    #endregion
}
