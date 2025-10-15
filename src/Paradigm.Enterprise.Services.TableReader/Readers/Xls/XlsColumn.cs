using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xls;

internal class XlsColumn : ColumnBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XlsColumn"/> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public XlsColumn(int index, string name, Type type) : base(index, name, type)
    {
    }
}