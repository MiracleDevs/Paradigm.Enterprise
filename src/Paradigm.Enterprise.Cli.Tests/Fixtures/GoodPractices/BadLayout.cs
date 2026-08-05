namespace GoodPractices;

internal sealed class BadLayout
{
    #region Properties

    public string Name { get; } = string.Empty;

    #endregion

    #region Field

    private readonly int _count;

    #endregion
}
