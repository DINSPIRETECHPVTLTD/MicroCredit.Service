using Microsoft.Data.SqlClient;

/// <summary>
/// Step 1 – Transfers each investor's investment amount to the owner user so the
///          owner holds the total pool (LedgerTransaction type = 'Remittance').
/// Step 2 – For every loan, records the owner disbursing the loan amount to the
///          member's loan (LedgerTransaction type = 'Loan').
/// Both steps are idempotent — re-running only adds missing records.
/// </summary>
public class LedgerPostingImporter
{
    private readonly SqlConnection _conn;
    private readonly int _importUserId;  // the owner who holds the pool

    public LedgerPostingImporter(SqlConnection conn, int importUserId)
    {
        _conn         = conn;
        _importUserId = importUserId;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n[LEDGER] Starting ledger postings...");

        await TransferInvestmentsToOwnerAsync();
        await PostLoanDisbursementsAsync();

        Console.WriteLine("[LEDGER] Done.");
    }

    // ── Step 1: investor → owner transfers ───────────────────────────────────

    private async Task TransferInvestmentsToOwnerAsync()
    {
        Console.WriteLine("\n[LEDGER] Step 1 — Transfer investments to owner...");

        // Get all investors with their investment totals
        var investors = new List<(int UserId, int InvestmentId, decimal Amount)>();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT i.UserId, i.Id, i.Amount
                FROM   Investments i
                JOIN   Users u ON u.Id = i.UserId
                WHERE  u.Email LIKE '%@navyafinservices.com'
                  AND  u.Id <> @ownerId";
            cmd.Parameters.AddWithValue("@ownerId", _importUserId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                investors.Add((r.GetInt32(0), r.GetInt32(1), r.GetDecimal(2)));
        }

        Console.WriteLine($"[LEDGER] Found {investors.Count} investor investment(s) to transfer.");

        int created = 0, skipped = 0;
        foreach (var (investorId, investmentId, amount) in investors)
        {
            // Idempotency check
            using var chk = _conn.CreateCommand();
            chk.CommandText = @"
                SELECT COUNT(1) FROM LedgerTransactions
                WHERE PaidFromUserId = @from AND PaidToUserId = @to
                  AND TransactionType = 'Remittance' AND ReferenceId = @refId";
            chk.Parameters.AddWithValue("@from",  investorId);
            chk.Parameters.AddWithValue("@to",    _importUserId);
            chk.Parameters.AddWithValue("@refId", investmentId);
            if (Convert.ToInt32(await chk.ExecuteScalarAsync()) > 0)
            {
                Console.WriteLine($"  [FOUND]   remittance tx already exists for investment id={investmentId}, skipped.");
                skipped++;
                continue;
            }

            var now = DateTime.UtcNow;

            // Create Remittance transaction: investor → owner
            using var tx = _conn.CreateCommand();
            tx.CommandText = @"
                INSERT INTO LedgerTransactions
                    (PaidFromUserId, PaidToUserId, Amount, PaymentDate,
                     CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
                OUTPUT INSERTED.Id
                VALUES (@from, @to, @amount, @date,
                        @createdBy, @date, 'Remittance', @refId, @comments)";
            tx.Parameters.AddWithValue("@from",      investorId);
            tx.Parameters.AddWithValue("@to",        _importUserId);
            tx.Parameters.AddWithValue("@amount",    amount);
            tx.Parameters.AddWithValue("@date",      now);
            tx.Parameters.AddWithValue("@createdBy", _importUserId);
            tx.Parameters.AddWithValue("@refId",     investmentId);
            tx.Parameters.AddWithValue("@comments",  $"Investment transfer to owner from investor id={investorId}");
            var txId = Convert.ToInt32(await tx.ExecuteScalarAsync());

            // Deduct from investor's ledger
            await UpsertLedgerAsync(investorId, -amount);

            // Credit to owner's ledger
            await UpsertLedgerAsync(_importUserId, amount);

            Console.WriteLine($"  [CREATED] remittance tx id={txId}  investorId={investorId}  amount={amount:N0} → ownerId={_importUserId}");
            created++;
        }

        Console.WriteLine($"[LEDGER] Step 1 done.  created={created}  skipped={skipped}");
    }

    // ── Step 2: owner → loan disbursements ───────────────────────────────────

    private async Task PostLoanDisbursementsAsync()
    {
        Console.WriteLine("\n[LEDGER] Step 2 — Post loan disbursements from owner...");

        // Get all loans created by the import user that don't yet have a Loan tx
        var loans = new List<(int LoanId, decimal Amount, DateTime DisbDate)>();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT l.Id, l.LoanAmount, l.DisbursementDate
                FROM   Loans l
                WHERE  l.CreatedBy = @createdBy
                  AND  NOT EXISTS (
                        SELECT 1 FROM LedgerTransactions lt
                        WHERE  lt.TransactionType = 'Loan'
                          AND  lt.ReferenceId     = l.Id
                          AND  lt.PaidFromUserId  = @ownerId)";
            cmd.Parameters.AddWithValue("@createdBy", _importUserId);
            cmd.Parameters.AddWithValue("@ownerId",   _importUserId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                loans.Add((r.GetInt32(0), r.GetDecimal(1), r.GetDateTime(2)));
        }

        Console.WriteLine($"[LEDGER] Found {loans.Count} loan(s) needing disbursement tx.");

        int created = 0;
        foreach (var (loanId, amount, disbDate) in loans)
        {
            using var tx = _conn.CreateCommand();
            tx.CommandText = @"
                INSERT INTO LedgerTransactions
                    (PaidFromUserId, PaidToUserId, Amount, PaymentDate,
                     CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
                OUTPUT INSERTED.Id
                VALUES (@from, NULL, @amount, @date,
                        @createdBy, @now, 'Loan', @refId, @comments)";
            tx.Parameters.AddWithValue("@from",      _importUserId);
            tx.Parameters.AddWithValue("@amount",    amount);
            tx.Parameters.AddWithValue("@date",      disbDate);
            tx.Parameters.AddWithValue("@createdBy", _importUserId);
            tx.Parameters.AddWithValue("@now",       DateTime.UtcNow);
            tx.Parameters.AddWithValue("@refId",     loanId);
            tx.Parameters.AddWithValue("@comments",  $"Loan disbursement for loan id={loanId}");
            var txId = Convert.ToInt32(await tx.ExecuteScalarAsync());

            // Deduct from owner's ledger
            await UpsertLedgerAsync(_importUserId, -amount);

            if (created % 50 == 0)
                Console.WriteLine($"  ... {created} loans posted so far (latest tx id={txId}, loan id={loanId}, amount={amount:N0})");
            created++;
        }

        Console.WriteLine($"[LEDGER] Step 2 done.  loan disbursement txs created={created}");
    }

    // ── Ledger balance upsert ─────────────────────────────────────────────────

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
        // Check existing balance
        using var chk = _conn.CreateCommand();
        chk.CommandText = "SELECT Id, Amount FROM Ledgers WHERE UserId = @userId";
        chk.Parameters.AddWithValue("@userId", userId);
        using var r = await chk.ExecuteReaderAsync();
        if (await r.ReadAsync())
        {
            var ledgerId = r.GetInt32(0);
            var current  = r.GetDecimal(1);
            r.Close();
            using var upd = _conn.CreateCommand();
            upd.CommandText = "UPDATE Ledgers SET Amount = @amount WHERE Id = @id";
            upd.Parameters.AddWithValue("@amount", current + delta);
            upd.Parameters.AddWithValue("@id",     ledgerId);
            await upd.ExecuteNonQueryAsync();
        }
        else
        {
            r.Close();
            using var ins = _conn.CreateCommand();
            ins.CommandText = "INSERT INTO Ledgers (UserId, Amount) VALUES (@userId, @amount)";
            ins.Parameters.AddWithValue("@userId", userId);
            ins.Parameters.AddWithValue("@amount", delta);
            await ins.ExecuteNonQueryAsync();
        }
    }
}
