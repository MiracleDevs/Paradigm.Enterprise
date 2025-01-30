namespace Paradigm.Enterprise.Providers.Extensions;

public static class StreamExtensions
{
    /// <summary>
    /// Converts to array.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns></returns>
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
    /// Converts to array.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns></returns>
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
    /// Converts to string.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public static string ConvertToString(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public static async Task<string> ConvertToStringAsync(this Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}