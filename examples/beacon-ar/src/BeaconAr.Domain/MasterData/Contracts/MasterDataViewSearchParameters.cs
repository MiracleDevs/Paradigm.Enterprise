using Paradigm.Enterprise.Domain.Dtos;

namespace BeaconAr.Domain.MasterData.Contracts;

public sealed class MasterDataViewSearchParameters : PaginationParametersBase
{
    #region Properties

    public bool? Active { get; set; }

    public int? CustomerId { get; set; }

    public string? Search { get; set; }

    public string? Type { get; set; }

    public AddressUsage? Usage { get; set; }

    #endregion
}
