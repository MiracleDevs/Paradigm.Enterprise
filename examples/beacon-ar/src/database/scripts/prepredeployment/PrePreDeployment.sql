-- Executable no-op keeps the SQLCMD-compatible pre-plan path continuously exercised.
SET NOCOUNT ON;
SELECT 1 WHERE 1 = 0;
GO
