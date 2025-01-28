using System.Data;

namespace Paradigm.Enterprise.Data.StoredProcedures.Mappers;

public interface IDataReaderMapper
{
    object Map(IDataReader reader);
}