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

        // Investments already credit ImportUser's ledger directly at creation time.
        // Only need to post loan disbursements (deducting from ImportUser).
        await PostLoanDisbursementsAsync();

        Console.WriteLine("[LEDGER] Done.");
    }

    // ── Loan disbursements: ImportUser → each loan ────────────────────────────

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
