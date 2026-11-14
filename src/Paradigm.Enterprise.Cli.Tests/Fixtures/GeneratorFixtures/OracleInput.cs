namespace MapperFixture;

public sealed class OracleInput
{
    #region Properties

    public int CategoryId { get; set; }
    public Guid Token { get; set; }
    public Guid? OptionalToken { get; set; }
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? SinceUtc { get; set; }
    public bool Enabled { get; set; }
    public bool? OptionalEnabled { get; set; }
    public DateTimeOffset? Offset { get; set; }
    public TimeSpan? Duration { get; set; }
    public byte[]? Payload { get; set; }
    public char Letter { get; set; }
    public char? OptionalLetter { get; set; }

    #endregion
}
