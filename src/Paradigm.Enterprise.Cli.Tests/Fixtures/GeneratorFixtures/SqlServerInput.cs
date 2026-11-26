namespace MapperFixture;

public sealed class SqlServerInput
{
    #region Properties

    public int CategoryId { get; set; }
    public string? Name { get; set; }
    public IEnumerable<SqlServerInputRow> Rows { get; set; } = [];

    #endregion
}
