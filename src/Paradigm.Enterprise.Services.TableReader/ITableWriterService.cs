using Paradigm.Enterprise.Services.Core;
using Paradigm.Enterprise.Services.TableReader.Models;

namespace Paradigm.Enterprise.Services.TableReader;

public interface ITableWriterService : IService
{
    /// <summary>
    /// Writes data to the specified stream.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="targetStream">The target stream to write to.</param>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns></returns>
    Task WriteToStreamAsync<T>(Stream targetStream, TableWriterParameters<T> parameters);

    /// <summary>
    /// Writes data and returns as byte array.
    /// </summary>
    /// <typeparam name="T">The type of the data items.</typeparam>
    /// <param name="parameters">The writer parameters.</param>
    /// <returns></returns>
    Task<byte[]> WriteToBytesAsync<T>(TableWriterParameters<T> parameters);
}

