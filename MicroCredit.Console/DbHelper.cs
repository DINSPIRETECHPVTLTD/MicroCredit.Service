using Microsoft.Data.SqlClient;
using System.Data;

/// <summary>
/// Wraps a connection string.
/// - <see cref="GetConn"/>: returns a long-lived shared connection, reconnecting if needed.
/// - <see cref="OpenAsync"/>: creates a SHORT-LIVED connection the caller owns and disposes.
///   Use this for high-frequency operations to avoid mid-read connection drops.
/// </summary>
public class DbHelper : IAsyncDisposable
{
    private readonly string _connStr;
    private SqlConnection? _conn;

    public DbHelper(string connStr)
    {
        _connStr = connStr;
    }

    /// <summary>
    /// Returns the shared open connection, reconnecting if it was dropped.
    /// Safe for low-frequency setup operations.
    /// </summary>
    public async Task<SqlConnection> GetConn()
    {
        if (_conn is { State: ConnectionState.Open })
            return _conn;

        if (_conn != null)
        {
            try { _conn.Close(); } catch { }
            await _conn.DisposeAsync();
        }

        _conn = new SqlConnection(_connStr);
        await _conn.OpenAsync();
        Console.WriteLine("[DB] (Re)connected.");
        return _conn;
    }

    /// <summary>
    /// Opens and returns a FRESH short-lived connection from the pool.
    /// The caller must dispose it (use with <c>await using</c>).
    /// Use this for high-frequency loops to avoid mid-read connection drops.
    /// </summary>
    public async Task<SqlConnection> OpenAsync()
    {
        var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        return conn;
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn != null)
        {
            try { _conn.Close(); } catch { }
            await _conn.DisposeAsync();
            _conn = null;
        }
    }
}
