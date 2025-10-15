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
    public ITableReader GetReaderInstance(Stream? sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration)
    {
        if (sourceStream is null)
            throw new ArgumentNullException(nameof(sourceStream));

        var originalPosition = sourceStream.CanSeek ? sourceStream.Position : 0;

        try
        {
            if (sourceStream.CanSeek)
                sourceStream.Position = 0;

            byte[] bytes;
            if (sourceStream is MemoryStream ms)
                bytes = ms.ToArray();
            else
            {
                using var memoryStream = new MemoryStream();
                sourceStream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            return GetReaderInstance(bytes, sourceHasHeader, configuration);
        }
        finally
        {
            // Restore original position if seekable
            if (sourceStream.CanSeek)
                sourceStream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceBytes">The source bytes.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    /// <exception cref="Exception">TableReader not found.</exception>
    public ITableReader GetReaderInstance(byte[]? sourceBytes, bool sourceHasHeader, TableReaderConfiguration configuration)
    {
        if (sourceBytes is null)
            throw new ArgumentNullException(nameof(sourceBytes));

        switch (configuration.TableReaderType)
        {
            case TableReaderTypes.Csv:
                return CsvTableReader.OpenFromContent(sourceBytes, sourceHasHeader, configuration.CsvParserConfiguration);

            case TableReaderTypes.Json:
                return JsonTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableReaderTypes.Xls:
                return XlsTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableReaderTypes.Xml:
                return XmlTableReader.OpenFromContent(sourceBytes, sourceHasHeader);
        }

        throw new Exception("TableReader not found.");
    }
}