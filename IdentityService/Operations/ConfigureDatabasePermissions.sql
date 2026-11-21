-- Run manually in IdentityDb after applying the IdentityService migrations.
-- Add the application login's user to this role outside this script.
-- Do not grant that user db_owner, db_ddladmin, sysadmin, or deployment credentials.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.SecurityAuditLogs', N'U') IS NULL
    THROW 50001, 'Apply IdentityService migrations in the dedicated IdentityDb first.', 1;

IF DATABASE_PRINCIPAL_ID(N'identity_runtime') IS NULL
    EXEC(N'CREATE ROLE [identity_runtime] AUTHORIZATION [dbo];');

DENY UPDATE, DELETE, ALTER ON OBJECT::dbo.SecurityAuditLogs TO identity_runtime;

COMMIT TRANSACTION;
