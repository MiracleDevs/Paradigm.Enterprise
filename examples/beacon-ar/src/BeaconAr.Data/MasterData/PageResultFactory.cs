using BeaconAr.Domain.MasterData.Contracts;

namespace BeaconAr.Data.MasterData;

internal static class PageResultFactory
{
    #region Public Methods

    public static PageResult<T> Create<T>(IReadOnlyList<T> items, int pageNumber, int pageSize, int count) =>
        new(items, pageNumber, pageSize, count == 0 ? 0 : (count + pageSize - 1) / pageSize, count);

    public static int? GetOffset(int pageNumber, int pageSize)
    {
        long offset = ((long)pageNumber - 1) * pageSize;
        return offset <= int.MaxValue ? (int)offset : null;
    }

    #endregion
}
