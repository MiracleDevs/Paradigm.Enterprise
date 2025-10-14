namespace Paradigm.Enterprise.Services.TableReader.Readers;

public interface IColumn
{
    /// <summary>
    /// Gets the column index.
    /// </summary>
    /// <value>
    /// The index.
    /// </value>
    int Index { get; }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    string Name { get; }

    /// <summary>
    /// Gets the column type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    Type Type { get; }
}