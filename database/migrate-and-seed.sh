#!/bin/bash
set -e

/opt/mssql-tools/bin/sqlcmd -S db -U SA -P "$DATABASE_PASSWORD" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ServiceLayer') BEGIN CREATE DATABASE ServiceLayer; END"
# /opt/mssql-tools/bin/sqlcmd -S db -d participant_database -U sa -P "$DATABASE_PASSWORD" -i /scripts/database/InitialTestData.sql //WP - does not exist yet
