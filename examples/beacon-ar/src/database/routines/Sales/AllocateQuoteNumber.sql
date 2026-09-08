CREATE PROCEDURE [dbo].[AllocateQuoteNumber]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT NEXT VALUE FOR [dbo].[QuoteNumberSequence];
END;
