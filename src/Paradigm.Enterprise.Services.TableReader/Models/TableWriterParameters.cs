using Paradigm.Enterprise.Services.TableReader.Configuration;

namespace Paradigm.Enterprise.Services.TableReader.Models;

/// <summary>
/// Parameters for table writer operations.
/// </summary>
/// <typeparam name="T">The type of the data items.</typeparam>
public class TableWriterParameters<T>
{
    /// <summary>
    /// Gets or sets the data to write.
    /// </summary>
    /// <value>
    /// The data.
    /// </value>
    public IEnumerable<T> Data { get; set; } = null!;

    /// <summary>
    /// Gets or sets the output format.
    /// </summary>
    /// <value>
    /// The format.
    /// </value>
    public TableFileTypes Format { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether [include header].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [include header]; otherwise, <c>false</c>.
    /// </value>
    public bool IncludeHeader { get; set; }

    /// <summary>
    /// Gets or sets the delegate function that extracts column values from each item.
    /// </summary>
    /// <value>
    /// The get column values.
    /// </value>
    public Func<T, IEnumerable<string?>> GetColumnValues { get; set; } = null!;

    /// <summary>
    /// Gets or sets the column names for the header. If null and includeHeader is true, column names will be auto-generated.
    /// </summary>
    /// <value>
    /// The column names.
    /// </value>
    public IEnumerable<string>? ColumnNames { get; set; }

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    /// <value>
    /// The configuration.
    /// </value>
    public TableConfiguration? Configuration { get; set; }
}

