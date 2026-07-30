using System.Data;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

/// <summary>
/// Maps the current row of a data reader to an application object.
/// </summary>
public interface IDataReaderMapper
{
    /// <summary>
    /// Maps the reader's current row without advancing the reader.
    /// </summary>
    /// <param name="reader">The reader positioned on the row to map.</param>
    /// <returns>The mapped application object.</returns>
    object Map(IDataReader reader);
}
