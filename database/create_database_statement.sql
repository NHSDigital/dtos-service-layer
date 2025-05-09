USE master;
GO

IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'Test'
        )
    CREATE DATABASE [Test];
GO

IF SERVERPROPERTY('ProductVersion') > '12'
    ALTER DATABASE [Test] SET QUERY_STORE = ON;
GO
