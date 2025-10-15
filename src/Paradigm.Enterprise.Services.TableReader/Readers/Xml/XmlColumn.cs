using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xml;

internal class XmlColumn : ColumnBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlColumn"/> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public XmlColumn(int index, string name, Type type) : base(index, name, type)
    {
    }
}