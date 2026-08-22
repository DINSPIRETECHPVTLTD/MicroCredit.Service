/*
  Test script for GET /report/staff-schedules-report/{branchId}
  Mirrors ReportService.GetStaffSchedulesReportByBranchAsync

  Set parameters, then run each section (or run all).
*/

SET NOCOUNT ON;

DECLARE @BranchId     INT      = 1;              -- change to your branch id
DECLARE @ScheduleDate DATE     = CAST(GETDATE() AS DATE);  -- or e.g. '2026-08-16'
DECLARE @WindowStart  DATETIME = @ScheduleDate;
DECLARE @WindowEnd    DATETIME = DATEADD(DAY, 1, @ScheduleDate);

PRINT 'BranchId=' + CAST(@BranchId AS varchar(20))
    + ', ScheduleDate=' + CONVERT(varchar(10), @ScheduleDate, 23);

/* =============================================================================
   1) GetPocCollectionStaffByBranchAsync — distinct collection staff
   ============================================================================= */
PRINT '';
PRINT '=== 1) STAFF (collection staff by branch) ===';

SELECT
    u.Id AS UserId,
    LTRIM(RTRIM(
        ISNULL(u.FirstName, N'') + N' ' +
        ISNULL(u.MiddleName, N'') + N' ' +
        ISNULL(u.LastName, N'')
    )) AS UserFullName,
    u.Role AS UserRole
FROM dinspire_sa.POCs p
INNER JOIN dinspire_sa.Users u ON p.CollectionBy = u.Id
INNER JOIN dinspire_sa.Centers c ON p.CenterId = c.Id
INNER JOIN dinspire_sa.Branchs b ON c.BranchId = b.Id
WHERE p.IsDeleted = 0
  AND b.IsDeleted = 0
  AND b.Id = @BranchId
GROUP BY u.Id, u.FirstName, u.MiddleName, u.LastName, u.Role
ORDER BY u.Id;

/* =============================================================================
   2) GetStaffReportPocsByBranchAsync — POCs with collecting staff
   ============================================================================= */
PRINT '';
PRINT '=== 2) POCS (by branch, with staff + center) ===';

SELECT
    p.Id AS PocId,
    p.CollectionBy AS PocStaffId,
    u.Id AS UserId,
    LTRIM(RTRIM(
        ISNULL(p.FirstName, N'') + N' ' +
        ISNULL(p.MiddleName, N'') + N' ' +
        ISNULL(p.LastName, N'')
    )) AS PocFullName,
    LTRIM(RTRIM(
        ISNULL(u.FirstName, N'') + N' ' +
        ISNULL(u.MiddleName, N'') + N' ' +
        ISNULL(u.LastName, N'')
    )) AS UserFullName,
    p.CenterId,
    c.Name AS CenterName,
    b.Id AS BranchId,
    u.Role AS UserRole
FROM dinspire_sa.POCs p
INNER JOIN dinspire_sa.Users u ON p.CollectionBy = u.Id
INNER JOIN dinspire_sa.Centers c ON p.CenterId = c.Id
INNER JOIN dinspire_sa.Branchs b ON c.BranchId = b.Id
WHERE p.IsDeleted = 0
  AND b.IsDeleted = 0
  AND b.Id = @BranchId
ORDER BY u.Id, p.Id;

/* =============================================================================
   3) GetStaffReportMembersByBranchAsync — member schedule lines for selected day
      Date filter: ScheduleDate on that day only (PaymentDate is returned, not used to filter)
   ============================================================================= */
PRINT '';
PRINT '=== 3) MEMBERS (schedule lines for selected day) ===';

SELECT
    m.Id AS MemberId,
    m.MemberCode,
    m.POCId AS PocId,
    LTRIM(RTRIM(
        ISNULL(m.FirstName, N'') + N' ' +
        ISNULL(m.MiddleName, N'') + N' ' +
        ISNULL(m.LastName, N'')
    )) AS MemberFullName,
    l.Id AS LoanId,
    l.Status AS LoanStatus,
    ls.LoanSchedulerId,
    ls.ScheduleDate,
    ls.PaymentDate,
    c.Name AS CenterName,
    CASE
        WHEN ls.Status = N'Partial'
            THEN CASE WHEN ls.ActualEmiAmount - ls.PaymentAmount > 0
                      THEN ls.ActualEmiAmount - ls.PaymentAmount
                      ELSE 0 END
        ELSE ls.ActualEmiAmount
    END AS ActualEmiAmount,
    ls.Status AS LoanSchedulerStatus
FROM dinspire_sa.Members m
INNER JOIN dinspire_sa.Loans l ON m.Id = l.MemberId
INNER JOIN dinspire_sa.LoanSchedulers ls ON l.Id = ls.LoanId
INNER JOIN dinspire_sa.Centers c ON m.CenterId = c.Id
INNER JOIN dinspire_sa.Branchs b ON c.BranchId = b.Id
WHERE m.IsDeleted = 0
  AND l.IsDeleted = 0
  AND b.IsDeleted = 0
  AND b.Id = @BranchId
  AND l.ClosureDate IS NULL
  AND LOWER(l.Status) = N'active'
  AND ls.ScheduleDate >= @WindowStart
  AND ls.ScheduleDate < @WindowEnd
ORDER BY m.POCId, m.Id, ls.ScheduleDate;

/* =============================================================================
   4) Combined flat view — Staff -> POC -> Member (like API hierarchy, one row)
   ============================================================================= */
PRINT '';
PRINT '=== 4) COMBINED (Staff -> POC -> Member) ===';

SELECT
    u.Id AS StaffUserId,
    LTRIM(RTRIM(
        ISNULL(u.FirstName, N'') + N' ' +
        ISNULL(u.MiddleName, N'') + N' ' +
        ISNULL(u.LastName, N'')
    )) AS StaffFullName,
    u.Role AS StaffRole,
    p.Id AS PocId,
    LTRIM(RTRIM(
        ISNULL(p.FirstName, N'') + N' ' +
        ISNULL(p.MiddleName, N'') + N' ' +
        ISNULL(p.LastName, N'')
    )) AS PocFullName,
    p.CenterId,
    c.Name AS CenterName,
    m.Id AS MemberId,
    m.MemberCode,
    LTRIM(RTRIM(
        ISNULL(m.FirstName, N'') + N' ' +
        ISNULL(m.MiddleName, N'') + N' ' +
        ISNULL(m.LastName, N'')
    )) AS MemberFullName,
    l.Id AS LoanId,
    ls.LoanSchedulerId,
    ls.ScheduleDate,
    ls.PaymentDate,
    CASE
        WHEN ls.Status = N'Partial'
            THEN CASE WHEN ls.ActualEmiAmount - ls.PaymentAmount > 0
                      THEN ls.ActualEmiAmount - ls.PaymentAmount
                      ELSE 0 END
        ELSE ls.ActualEmiAmount
    END AS ActualEmiAmount,
    ls.Status AS LoanSchedulerStatus
FROM dinspire_sa.POCs p
INNER JOIN dinspire_sa.Users u ON p.CollectionBy = u.Id
INNER JOIN dinspire_sa.Centers c ON p.CenterId = c.Id
INNER JOIN dinspire_sa.Branchs b ON c.BranchId = b.Id
LEFT JOIN dinspire_sa.Members m
    ON m.POCId = p.Id
   AND m.IsDeleted = 0
LEFT JOIN dinspire_sa.Loans l
    ON l.MemberId = m.Id
   AND l.IsDeleted = 0
   AND l.ClosureDate IS NULL
   AND LOWER(l.Status) = N'active'
LEFT JOIN dinspire_sa.LoanSchedulers ls
    ON ls.LoanId = l.Id
   AND ls.ScheduleDate >= @WindowStart
   AND ls.ScheduleDate < @WindowEnd
WHERE p.IsDeleted = 0
  AND b.IsDeleted = 0
  AND b.Id = @BranchId
ORDER BY StaffFullName, PocFullName, MemberFullName, ls.ScheduleDate;
