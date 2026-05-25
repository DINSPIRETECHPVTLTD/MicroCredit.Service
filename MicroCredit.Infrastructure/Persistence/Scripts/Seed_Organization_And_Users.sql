/*
  Bootstrap script for a NEW database (after EF migrations applied).

  Creates:
    - Organizations.Id = 1  (default org)
    - Users.Id = 1          (Owner at Org level; self-referencing CreatedBy)

  Default sign-in (change password immediately):
    Email:    owner@localhost
    Password: ChangeMeOnFirstLogin!1

  BCrypt hash below matches BCrypt.Net-Next used by MicroCredit.Application (UsersService / AuthService).

  Customize @OrgName, @OwnerEmail, @OwnerFirstName, @OwnerLastName before running if desired.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @OrgName nvarchar(200) = N'Navya Financial Services';
DECLARE @OwnerEmail nvarchar(200) = N'owner@navyafinservices.com';
DECLARE @OwnerFirstName nvarchar(100) = N'System';
DECLARE @OwnerLastName nvarchar(100) = N'Owner';
-- BCrypt (work factor 11) for: ChangeMeOnFirstLogin!1
DECLARE @PasswordHash nvarchar(max) = N'$2a$11$M4.6CWcLn9xcIzQtddGf3eByBZhrgQe0CHtksgVJT//WRRnpFUkEe';

IF EXISTS (SELECT 1 FROM dinspire_sa.Organizations WHERE Id = 1)
BEGIN
    RAISERROR ('Organization Id = 1 already exists. Remove it or adjust this script.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dinspire_sa.Users WHERE Id = 1 OR Email = @OwnerEmail)
BEGIN
    RAISERROR ('User Id = 1 or duplicate email already exists. Remove rows or adjust this script.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

-- Breaks circular FKs: Organization.CreatedBy -> User, User.CreatedBy -> User (self)
ALTER TABLE dinspire_sa.Organizations NOCHECK CONSTRAINT FK_Organizations_Users_CreatedBy;
ALTER TABLE dinspire_sa.Organizations NOCHECK CONSTRAINT FK_Organizations_Users_ModifiedBy;
ALTER TABLE dinspire_sa.Users NOCHECK CONSTRAINT FK_Users_Users_CreatedBy;
ALTER TABLE dinspire_sa.Users NOCHECK CONSTRAINT FK_Users_Users_ModifiedBy;

SET IDENTITY_INSERT dinspire_sa.Organizations ON;

INSERT INTO dinspire_sa.Organizations
(
    Id,
    Name,
    Address1,
    Address2,
    City,
    State,
    ZipCode,
    PhoneNumber,
    CreatedBy,
    CreatedAt,
    ModifiedBy,
    ModifiedAt,
    IsDeleted
)
VALUES
(
    1,
    @OrgName,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1,
    SYSUTCDATETIME(),
    NULL,
    NULL,
    0
);

SET IDENTITY_INSERT dinspire_sa.Organizations OFF;

SET IDENTITY_INSERT dinspire_sa.Users ON;

INSERT INTO dinspire_sa.Users
(
    Id,
    FirstName,
    MiddleName,
    LastName,
    Role,
    Email,
    PhoneNumber,
    Address1,
    Address2,
    City,
    State,
    ZipCode,
    OrgId,
    Level,
    BranchId,
    PasswordHash,
    CreatedBy,
    CreatedAt,
    ModifiedBy,
    ModifiedAt,
    IsDeleted
)
VALUES
(
    1,
    @OwnerFirstName,
    NULL,
    @OwnerLastName,
    N'Owner',
    @OwnerEmail,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1,
    N'Org',
    NULL,
    @PasswordHash,
    1,
    SYSUTCDATETIME(),
    NULL,
    NULL,
    0
);

SET IDENTITY_INSERT dinspire_sa.Users OFF;

ALTER TABLE dinspire_sa.Organizations CHECK CONSTRAINT FK_Organizations_Users_CreatedBy;
ALTER TABLE dinspire_sa.Organizations CHECK CONSTRAINT FK_Organizations_Users_ModifiedBy;
ALTER TABLE dinspire_sa.Users CHECK CONSTRAINT FK_Users_Users_CreatedBy;
ALTER TABLE dinspire_sa.Users CHECK CONSTRAINT FK_Users_Users_ModifiedBy;

COMMIT TRANSACTION;

PRINT N'Seed complete: Organizations (1), Users (1). Sign in with email ''' + @OwnerEmail + N''' and password ChangeMeOnFirstLogin!1 — change password on first use.';
