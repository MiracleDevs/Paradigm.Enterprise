CREATE PROCEDURE [dbo].[GetOrCreateApplicationUser]
    @Issuer NVARCHAR(400),
    @Subject NVARCHAR(200),
    @DisplayName NVARCHAR(200),
    @Email NVARCHAR(320) = NULL,
    @Now DATETIMEOFFSET(7)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Id INT;
    SELECT @Id = [Id]
    FROM [dbo].[ApplicationUser] WITH (UPDLOCK, HOLDLOCK)
    WHERE [Issuer] = @Issuer AND [Subject] = @Subject;

    IF @Id IS NULL
    BEGIN
        INSERT INTO [dbo].[ApplicationUser]
            ([Issuer], [Subject], [DisplayName], [Email], [IsActive], [CreationDate])
        VALUES
            (@Issuer, @Subject, @DisplayName, @Email, 1, @Now);
        SET @Id = CONVERT(INT, SCOPE_IDENTITY());
    END;

    COMMIT TRANSACTION;
    SELECT @Id;
END;
