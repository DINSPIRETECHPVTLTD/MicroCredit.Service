using Microsoft.Data.SqlClient;

/// <summary>
/// Step 1 – Transfers each owner/investor's investment balance to ImportUser
///          (LedgerTransaction type = 'Remittance', PaidFrom = owner/investor, PaidTo = importUser).
/// Step 2 – For every loan, records ImportUser disbursing the loan amount to the member
///          (LedgerTransaction type = 'Loan', PaidFrom = importUser).
/// Both steps are idempotent — re-running only adds missing records.
/// </summary>
public class LedgerPostingImporter
{
    private readonly SqlConnection _conn;
    private readonly int _importUserId;

    public LedgerPostingImporter(SqlConnection conn, int importUserId)
    {
        _conn         = conn;
        _importUserId = importUserId;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n[LEDGER] Starting ledger postings...");
        await TransferInvestmentsToImportUserAsync();
        await PostLoanDisbursementsAsync();
        Console.WriteLine("[LEDGER] Done.");
    }

    // ── Step 1: Transfer all owner/investor investments → ImportUser ──────────

    private async Task TransferInvestmentsToImportUserAsync()
    {
        Console.WriteLine("\n[LEDGER] Step 1 — Transfer owner/investor investments to ImportUser...");

        // Get all investments where the investor/owner is NOT the importUser
        // and there is no existing Remittance tx from that user to importUser for this investment
        var investments = new List<(int InvestmentId, int UserId, decimal Amount, DateTime Date)>();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT i.Id, i.UserId, i.Amount, i.InvestmentDate
                FROM   Investments i
                WHERE  i.UserId <> @importUserId
                  AND  NOT EXISTS (
                        SELECT 1 FROM LedgerTransactions lt
                        WHERE  lt.TransactionType  = 'Remittance'
                          AND  lt.ReferenceId      = i.Id
                          AND  lt.PaidFromUserId   = i.UserId
                          AND  lt.PaidToUserId     = @importUserId)";
            cmd.Parameters.AddWithValue("@importUserId", _importUserId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                investments.Add((r.GetInt32(0), r.GetInt32(1), r.GetDecimal(2), r.GetDateTime(3)));
        }

        Console.WriteLine($"[LEDGER] Found {investments.Count} investment(s) to transfer.");

        int created = 0;
        foreach (var (invId, userId, amount, date) in investments)
        {
            // Record the transfer: owner/investor → ImportUser
            using var tx = _conn.CreateCommand();
            tx.CommandText = @"
                INSERT INTO LedgerTransactions
                    (PaidFromUserId, PaidToUserId, Amount, PaymentDate,
                     CreatedBy, CreatedDate, TransactionType, ReferenceId, Comments)
                OUTPUT INSERTED.Id
                VALUES (@from, @to, @amount, @date,
                        @createdBy, GETUTCDATE(), 'Remittance', @refId, @comments)";
            tx.Parameters.AddWithValue("@from",      userId);
            tx.Parameters.AddWithValue("@to",        _importUserId);
            tx.Parameters.AddWithValue("@amount",    amount);
            tx.Parameters.AddWithValue("@date",      date);
            tx.Parameters.AddWithValue("@createdBy", _importUserId);
            tx.Parameters.AddWithValue("@refId",     invId);
            tx.Parameters.AddWithValue("@comments",  $"Transfer investment id={invId} from userId={userId} to importUser");
            var txId = Convert.ToInt32(await tx.ExecuteScalarAsync());

            // Deduct from owner/investor's ledger
            await UpsertLedgerAsync(userId, -amount);

            // Credit ImportUser's ledger
            await UpsertLedgerAsync(_importUserId, amount);

            Console.WriteLine($"  [TRANSFER] inv={invId}  from={userId}  to=importUser({_importUserId})  amount={amount:N0}  tx={txId}");
            created++;
        }

        Console.WriteLine($"[LEDGER] Step 1 done.  transfers created={created}");
    }

    // ── Step 2: Loan disbursements: ImportUser → each loan ────────────────────

    private async Task PostLoanDisbursementsAsync()
    {
        Console.WriteLine("\n[LEDGER] Step 2 — Post loan disbursements from ImportUser...");

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
                        @createdBy, GETUTCDATE(), 'Loan', @refId, @comments)";
            tx.Parameters.AddWithValue("@from",      _importUserId);
            tx.Parameters.AddWithValue("@amount",    amount);
            tx.Parameters.AddWithValue("@date",      disbDate);
            tx.Parameters.AddWithValue("@createdBy", _importUserId);
            tx.Parameters.AddWithValue("@refId",     loanId);
            tx.Parameters.AddWithValue("@comments",  $"Loan disbursement for loan id={loanId}");
            var txId = Convert.ToInt32(await tx.ExecuteScalarAsync());

            // Deduct from ImportUser's ledger
            await UpsertLedgerAsync(_importUserId, -amount);

            if (created % 50 == 0)
                Console.WriteLine($"  ... {created} loans posted (latest tx={txId}, loan={loanId}, amount={amount:N0})");
            created++;
        }

        Console.WriteLine($"[LEDGER] Step 2 done.  loan disbursement txs created={created}");
    }

    // ── Ledger balance upsert ─────────────────────────────────────────────────

    private async Task UpsertLedgerAsync(int userId, decimal delta)
    {
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
