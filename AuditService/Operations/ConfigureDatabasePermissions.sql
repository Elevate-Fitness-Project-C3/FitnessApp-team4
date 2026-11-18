-- Run manually in the dedicated AuditDb after applying the AuditService migrations.
-- Provision a separate application user and add it to this role outside this script.
-- Do not grant that user db_owner, db_ddladmin, sysadmin, or deployment credentials.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.AuditEvents', N'U') IS NULL
    OR OBJECT_ID(N'dbo.AuditEventReceipts', N'U') IS NULL
    OR OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    OR OBJECT_ID(N'dbo.InboxState', N'U') IS NULL
    OR OBJECT_ID(N'dbo.OutboxMessage', N'U') IS NULL
    OR OBJECT_ID(N'dbo.OutboxState', N'U') IS NULL
    THROW 50001, 'Apply AuditService migrations in the dedicated AuditDb first.', 1;

IF DATABASE_PRINCIPAL_ID(N'audit_runtime') IS NULL
    EXEC(N'CREATE ROLE [audit_runtime] AUTHORIZATION [dbo];');

GRANT SELECT, INSERT ON OBJECT::dbo.AuditEvents TO audit_runtime;
GRANT SELECT, INSERT ON OBJECT::dbo.AuditEventReceipts TO audit_runtime;
GRANT SELECT ON OBJECT::dbo.__EFMigrationsHistory TO audit_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON OBJECT::dbo.InboxState TO audit_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON OBJECT::dbo.OutboxMessage TO audit_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON OBJECT::dbo.OutboxState TO audit_runtime;

DENY UPDATE, DELETE, ALTER ON OBJECT::dbo.AuditEvents TO audit_runtime;
DENY UPDATE, DELETE, ALTER ON OBJECT::dbo.AuditEventReceipts TO audit_runtime;

COMMIT TRANSACTION;
