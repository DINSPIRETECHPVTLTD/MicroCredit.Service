-- ============================================================
-- cleanup.sql
-- Deletes ALL records created by the MicroCredit.Console import.
-- Anchor: the import user identified by email.
--
-- FK-safe deletion order:
--   1. NULL out Users.BranchId   (breaks Users → Branchs cycle)
--   2. MemberMembershipFees → Members
--   3. Loans → Members
--   4. Members → Centers, POCs
--   5. LedgerTransactions, Ledgers, Investments
--   6. POCs (CollectionBy → Users, CenterId → Centers)
--   7. Centers (BranchId → Branchs)
--   8. Branchs (CreatedBy → Users, safe while import user still exists)
--   9. Users (last — all FK refs gone)
-- ============================================================

BEGIN TRANSACTION;

-- ── 0. Resolve import user id ────────────────────────────────
DECLARE @importUserId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'importuser@navyafinservices.com');

IF @importUserId IS NULL
BEGIN
    PRINT 'Import user not found — nothing to clean up.';
    ROLLBACK;
    RETURN;
END
PRINT CONCAT('Import user id = ', @importUserId);

-- ── Collect console-created user ids ─────────────────────────
CREATE TABLE #ConsoleUsers (Id INT PRIMARY KEY);
INSERT INTO #ConsoleUsers (Id)
SELECT Id FROM Users WHERE Email LIKE '%@navyafinservices.com';
PRINT CONCAT('Console user count = ', (SELECT COUNT(*) FROM #ConsoleUsers));

-- ── Break Users.BranchId → Branchs FK (BranchId is nullable) ─
UPDATE Users SET BranchId = NULL WHERE Id IN (SELECT Id FROM #ConsoleUsers);
PRINT 'Users.BranchId NULLed.';

-- ── 1. MemberMembershipFees ───────────────────────────────────
DELETE FROM MemberMembershipFees WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted MemberMembershipFees : ', @@ROWCOUNT);

-- ── 2. Loans ─────────────────────────────────────────────────
DELETE FROM Loans WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Loans                : ', @@ROWCOUNT);

-- ── 3. Members ───────────────────────────────────────────────
DELETE FROM Members WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Members              : ', @@ROWCOUNT);

-- ── 4. LedgerTransactions ────────────────────────────────────
DELETE FROM LedgerTransactions
WHERE PaidToUserId IN (SELECT Id FROM #ConsoleUsers)
   OR CreatedBy = @importUserId;
PRINT CONCAT('Deleted LedgerTransactions   : ', @@ROWCOUNT);

-- ── 5. Ledgers ───────────────────────────────────────────────
DELETE FROM Ledgers WHERE UserId IN (SELECT Id FROM #ConsoleUsers);
PRINT CONCAT('Deleted Ledgers              : ', @@ROWCOUNT);

-- ── 6. Investments ───────────────────────────────────────────
DELETE FROM Investments
WHERE UserId IN (SELECT Id FROM #ConsoleUsers)
   OR CreatedById = @importUserId;
PRINT CONCAT('Deleted Investments          : ', @@ROWCOUNT);

-- ── 7. POCs (before Centers and Users) ───────────────────────
DELETE FROM POCs WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted POCs                 : ', @@ROWCOUNT);

-- ── 8. Centers (before Branches) ─────────────────────────────
DELETE FROM Centers WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Centers              : ', @@ROWCOUNT);

-- ── 9. Branches (CreatedBy = import user who still exists) ───
DELETE FROM Branchs WHERE CreatedBy = @importUserId;
PRINT CONCAT('Deleted Branches             : ', @@ROWCOUNT);

-- ── 10. Users (last — all FK refs gone) ──────────────────────
DELETE FROM Users WHERE Id IN (SELECT Id FROM #ConsoleUsers);
PRINT CONCAT('Deleted Users                : ', @@ROWCOUNT);

DROP TABLE #ConsoleUsers;

-- ── Done ──────────────────────────────────────────────────────
PRINT '--------------------------------------------';
PRINT 'Cleanup complete. Review counts above.';
PRINT 'Run COMMIT to apply or ROLLBACK to undo.';

-- COMMIT;   -- uncomment to apply permanently
-- ROLLBACK; -- uncomment for dry-run
