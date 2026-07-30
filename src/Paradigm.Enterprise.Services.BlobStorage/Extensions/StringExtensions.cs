using System.Text;

namespace Paradigm.Enterprise.Services.BlobStorage.Extensions;

/// <summary>
/// Provides text helpers used by blob metadata and upload operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>Inserts spaces at camel-case word boundaries.</summary>
    /// <param name="input">The text to split.</param>
    /// <returns>The text with word boundaries separated by spaces.</returns>
    public static string SplitCamelCase(this string input)
    {
        var builder = new StringBuilder();

        foreach (var c in input)
        {
            if (char.IsUpper(c) && builder.Length > 0)
                builder.Append(' ');

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts to stream.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A readable UTF-8 stream positioned at the beginning. The caller must dispose it.</returns>
    /// <exception cref="ArgumentNullException">value</exception>
    public static Stream ToStream(this string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(value);
        writer.Flush();
        stream.Position = 0;

        return stream;
    }
}
