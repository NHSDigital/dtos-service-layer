USE master;
GO

IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'PathwayCoordinator'
        )
    CREATE DATABASE [PathwayCoordinator];
GO

IF SERVERPROPERTY('ProductVersion') > '12'
    ALTER DATABASE [PathwayCoordinator] SET QUERY_STORE = ON;
GO
