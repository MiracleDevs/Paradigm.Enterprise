namespace BeaconAr.Providers.MasterData;

internal static class OfficialMasterDataMutationGuard
{
    #region Public Methods

    public static NotSupportedException Create() => new(
        "Generated-view mutations are intentionally disabled until the Task 4 transport defines its concurrency and audit protocol. Use the request-based master-data operations.");

    #endregion
}
