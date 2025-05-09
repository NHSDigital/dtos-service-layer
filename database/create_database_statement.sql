USE master;
GO

-- Check if database exists and create it if it doesn't
IF NOT EXISTS (
        SELECT name
        FROM sys.databases
        WHERE name = N'ServiceLayer'
        )
BEGIN
    CREATE DATABASE [ServiceLayer];
END
GO

USE [ServiceLayer];
GO

-- Only try to set QUERY_STORE if SQL Server version is higher than 12 (SQL 2014)
IF CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128)) > '12'
BEGIN
    ALTER DATABASE [ServiceLayer] SET QUERY_STORE = ON;
END
GO
