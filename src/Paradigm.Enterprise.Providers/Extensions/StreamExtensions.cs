namespace Paradigm.Enterprise.Providers.Extensions;

/// <summary>
/// Reads the remaining content of a stream into memory.
/// </summary>
/// <remarks>All operations start at the stream's current position.</remarks>
public static class StreamExtensions
{
    /// <summary>
    /// Asynchronously reads the remaining bytes into a new array.
    /// </summary>
    /// <param name="stream">The readable stream to consume from its current position.</param>
    /// <returns>A new array containing all bytes read from the current position.</returns>
    /// <remarks>This method leaves <paramref name="stream"/> open.</remarks>
    public static async Task<byte[]> ToArrayAsync(this Stream stream)
    {
        var buffer = new byte[1024 * 1024];
        await using var ms = new MemoryStream();
        int read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
            ms.Write(buffer, 0, read);

        return ms.ToArray();
    }

    /// <summary>
    /// Reads the remaining bytes into a new array.
    /// </summary>
    /// <param name="stream">The readable stream to consume from its current position.</param>
    /// <returns>A new array containing all bytes read from the current position.</returns>
    /// <remarks>This method leaves <paramref name="stream"/> open.</remarks>
    public static byte[] ToArray(this Stream stream)
    {
        var buffer = new byte[1024 * 1024];
        using var ms = new MemoryStream();
        int read;

        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            ms.Write(buffer, 0, read);

        return ms.ToArray();
    }

    /// <summary>
    /// Reads the remaining text synchronously using the default <see cref="StreamReader"/> encoding.
    /// </summary>
    /// <param name="stream">The readable stream to consume from its current position.</param>
    /// <returns>The decoded text from the current position through the end of the stream.</returns>
    /// <remarks>Disposing the internal reader also disposes <paramref name="stream"/>.</remarks>
    public static string ConvertToString(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads the remaining text asynchronously using the default <see cref="StreamReader"/> encoding.
    /// </summary>
    /// <param name="stream">The readable stream to consume from its current position.</param>
    /// <returns>The decoded text from the current position through the end of the stream.</returns>
    /// <remarks>Disposing the internal reader also disposes <paramref name="stream"/>.</remarks>
    public static async Task<string> ConvertToStringAsync(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
