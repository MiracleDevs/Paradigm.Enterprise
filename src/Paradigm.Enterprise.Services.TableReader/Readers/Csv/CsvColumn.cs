using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Csv;

internal class CsvColumn : ColumnBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvColumn"/> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public CsvColumn(int index, string name, Type type) : base(index, name, type)
    {
    }
}