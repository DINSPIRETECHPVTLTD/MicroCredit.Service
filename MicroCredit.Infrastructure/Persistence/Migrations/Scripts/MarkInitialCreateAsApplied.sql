-- Run this script on your database if tables already exist but __EFMigrationsHistory
-- is missing the InitialCreate entry (so "database update" tries to create tables again).
-- Then run: dotnet ef database update --project MicroCredit.Infrastructure --startup-project MicroCredit.Api

-- Ensure the migrations history table exists (EF creates it on first run)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END
GO

-- Mark InitialCreate as already applied (so EF will skip it and only run UseNvarcharForRoleAndLevel)
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260228045952_InitialCreate')
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260228045952_InitialCreate', N'9.0.13');
GO
