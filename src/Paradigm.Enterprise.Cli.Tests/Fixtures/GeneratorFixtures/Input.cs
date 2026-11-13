namespace MapperFixture;

public sealed class Input
{
    #region Properties

    public int CategoryId { get; set; }
    public Guid Token { get; set; }
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? SinceUtc { get; set; }

    #endregion
}