using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.TableReader.Configuration;
using Paradigm.Enterprise.Services.TableReader.Readers;

namespace Paradigm.Enterprise.Services.TableReader;
public interface ITableReaderService : IService
{
    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceStream">The source stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    ITableReader GetReaderInstance(Stream sourceStream, bool sourceHasHeader, TableReaderConfiguration configuration);

    /// <summary>
    /// Gets the reader instance.
    /// </summary>
    /// <param name="sourceBytes">The source bytes.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns></returns>
    ITableReader GetReaderInstance(byte[] sourceBytes, bool sourceHasHeader, TableReaderConfiguration configuration);
}