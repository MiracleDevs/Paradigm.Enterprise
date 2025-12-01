using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Readers;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv;
using Paradigm.Enterprise.Services.TableReader.Readers.Json;
using Paradigm.Enterprise.Services.TableReader.Readers.Xls;
using Paradigm.Enterprise.Services.TableReader.Readers.Xml;

namespace Paradigm.Enterprise.Services.TableReader;

public class TableReaderService : ITableReaderService
{
    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceStream">The source stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    public ITableReader GetReaderInstance(Stream? sourceStream, bool sourceHasHeader, TableConfiguration configuration)
    {
        if (sourceStream is null)
            throw new ArgumentNullException(nameof(sourceStream));

        // Reset stream position if seekable
        if (sourceStream.CanSeek && sourceStream.Position != 0)
            sourceStream.Position = 0;

        switch (configuration.TableFileType)
        {
            case TableFileTypes.Csv:
                return CsvTableReader.OpenFromStream(sourceStream, sourceHasHeader, configuration.CsvParserConfiguration);

            case TableFileTypes.Json:
                return JsonTableReader.OpenFromStream(sourceStream, sourceHasHeader);

            case TableFileTypes.Xls:
                return XlsTableReader.OpenFromStream(sourceStream, sourceHasHeader);

            case TableFileTypes.Xml:
                return XmlTableReader.OpenFromStream(sourceStream, sourceHasHeader);
        }

        throw new Exception("TableReader not found.");
    }

    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceBytes">The source bytes.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    /// <exception cref="Exception">TableReader not found.</exception>
    public ITableReader GetReaderInstance(byte[]? sourceBytes, bool sourceHasHeader, TableConfiguration configuration)
    {
        if (sourceBytes is null)
            throw new ArgumentNullException(nameof(sourceBytes));

        switch (configuration.TableFileType)
        {
            case TableFileTypes.Csv:
                return CsvTableReader.OpenFromContent(sourceBytes, sourceHasHeader, configuration.CsvParserConfiguration);

            case TableFileTypes.Json:
                return JsonTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableFileTypes.Xls:
                return XlsTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableFileTypes.Xml:
                return XmlTableReader.OpenFromContent(sourceBytes, sourceHasHeader);
        }

        throw new Exception("TableReader not found.");
    }
}