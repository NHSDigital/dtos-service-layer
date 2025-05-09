USE master;
GO

IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'ServiceLayer'
        )
    CREATE DATABASE [Test];
GO

IF SERVERPROPERTY('ProductVersion') > '12'
    ALTER DATABASE [ServiceLayer] SET QUERY_STORE = ON;
GO
