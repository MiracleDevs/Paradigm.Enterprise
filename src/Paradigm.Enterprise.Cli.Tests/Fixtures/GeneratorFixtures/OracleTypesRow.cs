namespace MapperFixture;

public sealed class OracleTypesRow
{
    #region Properties

    public DateTimeOffset Offset { get; set; }
    public TimeSpan Duration { get; set; }
    public char Letter { get; set; }
    public char? OptionalLetter { get; set; }
    public Guid? OptionalToken { get; set; }
    public bool? OptionalEnabled { get; set; }

    #endregion
}
