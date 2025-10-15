using Paradigm.Enterprise.Services.TableReader.Readers.Base;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Json;

internal class JsonColumn : ColumnBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonColumn"/> class.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    public JsonColumn(int index, string name, Type type) : base(index, name, type)
    {
    }
}