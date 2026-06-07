-- ============================================================
-- cleanup.sql
-- Deletes ALL records created by the MicroCredit.Console import.
-- Anchor: the import user identified by email.
-- Run inside a transaction so you can ROLLBACK if needed.
-- ============================================================

BEGIN TRANSACTION;

-- ── 0. Resolve import user id ────────────────────────────────
DECLARE @importEmail   NVARCHAR(200) = 'importuser@navyafinservices.com';
DECLARE @importUserId  INT = (SELECT Id FROM Users WHERE Email = @importEmail);

IF @importUserId IS NULL
BEGIN
    PRINT 'Import user not found — nothing to clean up.';
    ROLLBACK;
    RETURN;
END
PRINT CONCAT('Import user id = ', @importUserId);

-- ── Collect all console-created user ids ─────────────────────
-- All users whose email ends with @navyafinservices.com were
-- created exclusively by the console (owner, investor, staff).
DECLARE @consoleUserIds TABLE (Id INT PRIMARY KEY);
INSERT INTO @consoleUserIds (Id)
SELECT Id FROM Users
WHERE Email LIKE '%@navyafinservices.com' AND IsDeleted IN (0, 1);

PRINT CONCAT('Console user count = ', (SELECT COUNT(*) FROM @consoleUserIds));

-- ── 1. MemberMembershipFees (created by import user) ─────────
DELETE FROM MemberMembershipFees
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted MemberMembershipFees: ', @@ROWCOUNT);

-- ── 2. Loans (created by import user) ────────────────────────
DELETE FROM Loans
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Loans: ', @@ROWCOUNT);

-- ── 3. Members (created by import user) ──────────────────────
DELETE FROM Members
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Members: ', @@ROWCOUNT);

-- ── 4. LedgerTransactions for console investors ───────────────
DELETE FROM LedgerTransactions
WHERE PaidToUserId IN (SELECT Id FROM @consoleUserIds)
   OR CreatedBy    = @importUserId;
PRINT CONCAT('Deleted LedgerTransactions: ', @@ROWCOUNT);

-- ── 5. Ledger balances for console investors ──────────────────
DELETE FROM Ledgers
WHERE UserId IN (SELECT Id FROM @consoleUserIds);
PRINT CONCAT('Deleted Ledgers: ', @@ROWCOUNT);

-- ── 6. Investments for console investors ─────────────────────
DELETE FROM Investments
WHERE UserId IN (SELECT Id FROM @consoleUserIds)
   OR CreatedById = @importUserId;
PRINT CONCAT('Deleted Investments: ', @@ROWCOUNT);

-- ── 7. POCs created by import user ───────────────────────────
DELETE FROM POCs
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted POCs: ', @@ROWCOUNT);

-- ── 8. Centers created by import user ────────────────────────
DELETE FROM Centers
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Centers: ', @@ROWCOUNT);

-- ── 9. Branches created by import user ───────────────────────
DELETE FROM Branchs
WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Branches: ', @@ROWCOUNT);

-- ── 10. All console-created users ────────────────────────────
DELETE FROM Users
WHERE Id IN (SELECT Id FROM @consoleUserIds);
PRINT CONCAT('Deleted Users: ', @@ROWCOUNT);

-- ── Summary ──────────────────────────────────────────────────
PRINT '--------------------------------------------';
PRINT 'Cleanup complete. Review counts above.';
PRINT 'Run COMMIT to apply or ROLLBACK to undo.';

-- COMMIT;   -- uncomment when ready to apply permanently
-- ROLLBACK; -- uncomment to undo (safe dry-run)
