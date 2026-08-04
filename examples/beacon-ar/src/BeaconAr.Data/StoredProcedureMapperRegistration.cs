using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using BeaconAr.Data.Access.Mappers;
using BeaconAr.Data.MasterData.Mappers;
using BeaconAr.Data.Operations.Mappers;
using BeaconAr.Data.Sales.Mappers;

namespace BeaconAr.Data;

internal static class StoredProcedureMapperRegistration
{
    #region Public Methods

    [ModuleInitializer]
    [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute is only intended to be used in application code or advanced source generator scenarios", Justification = "Generated stored-procedure mappers must be registered before any repository executes a typed routine.")]
    public static void Register()
    {
        AccessStoredProcedureMappersRegisterer.RegisterMappers();
        MasterDataStoredProcedureMappersRegisterer.RegisterMappers();
        OperationsStoredProcedureMappersRegisterer.RegisterMappers();
        SalesStoredProcedureMappersRegisterer.RegisterMappers();
    }

    #endregion
}
