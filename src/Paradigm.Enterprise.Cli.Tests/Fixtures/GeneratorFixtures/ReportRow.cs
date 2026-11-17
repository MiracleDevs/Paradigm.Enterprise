namespace MapperFixture;

public sealed class ReportRow
{
    #region Properties

    public int Id { get; set; }
    public Guid Token { get; set; }
    public string? Name { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }
    public bool Enabled { get; set; }
    public byte[] Payload { get; set; } = [];

    #endregion
}