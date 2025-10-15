namespace Paradigm.Enterprise.Services.TableReader.Readers.Base;

internal abstract class ColumnBase : IColumn
{
    #region Properties

    /// <summary>
    /// Gets the column index.
    /// </summary>
    /// <value>
    /// The index.
    /// </value>
    public int Index { get; }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the column type.
    /// </summary>
    /// <value>
    /// The type.
    /// </value>
    public Type Type { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnBase"/> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    internal ColumnBase(int index, string name, Type type)
    {
        Index = index;
        Name = name;
        Type = type;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts to string.
    /// </summary>
    /// <returns>
    /// A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString()
    {
        return $"{Index} - {Name}[{Type.Name}]";
    }

    #endregion
}