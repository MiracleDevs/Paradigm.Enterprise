using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Readers;
using Paradigm.Enterprise.Services.TableReader.Readers.Csv;
using Paradigm.Enterprise.Services.TableReader.Readers.Json;
using Paradigm.Enterprise.Services.TableReader.Readers.Xls;
using Paradigm.Enterprise.Services.TableReader.Readers.Xml;

namespace Paradigm.Enterprise.Services.TableReader;

/// <summary>
/// Selects a CSV, Excel, XML, or JSON table reader from a <see cref="TableConfiguration"/>.
/// </summary>
/// <remarks>
/// Register this implementation behind <see cref="ITableReaderService"/>. Readers are disposable;
/// caller-provided streams remain caller-owned and are left open.
/// </remarks>
/// <example>
/// <code>
/// services.AddSingleton&lt;ITableReaderService, TableReaderService&gt;();
/// </code>
/// See <see cref="ITableReaderService.GetReaderInstance(Stream, bool, TableConfiguration)"/> for
/// sequential reading and stream-lifetime usage.
/// </example>
public class TableReaderService : ITableReaderService
{
    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sourceStream"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A seekable stream is rewound before parsing. The caller retains ownership of the stream and
    /// must keep it open while the reader is in use; disposing the reader leaves the stream open.
    /// </remarks>
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

            case TableFileTypes.Xls:
                return XlsTableReader.OpenFromStream(sourceStream, sourceHasHeader);

            case TableFileTypes.Xml:
                return XmlTableReader.OpenFromStream(sourceStream, sourceHasHeader);

            case TableFileTypes.Json:
                return JsonTableReader.OpenFromStream(sourceStream, sourceHasHeader);
        }

        throw new Exception("TableReader not found.");
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sourceBytes"/> is <see langword="null"/>.</exception>
    /// <exception cref="Exception"><see cref="TableConfiguration.TableFileType"/> is not supported.</exception>
    public ITableReader GetReaderInstance(byte[]? sourceBytes, bool sourceHasHeader, TableConfiguration configuration)
    {
        if (sourceBytes is null)
            throw new ArgumentNullException(nameof(sourceBytes));

        switch (configuration.TableFileType)
        {
            case TableFileTypes.Csv:
                return CsvTableReader.OpenFromContent(sourceBytes, sourceHasHeader, configuration.CsvParserConfiguration);

            case TableFileTypes.Xls:
                return XlsTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableFileTypes.Xml:
                return XmlTableReader.OpenFromContent(sourceBytes, sourceHasHeader);

            case TableFileTypes.Json:
                return JsonTableReader.OpenFromContent(sourceBytes, sourceHasHeader);
        }

        throw new Exception("TableReader not found.");
    }
}
