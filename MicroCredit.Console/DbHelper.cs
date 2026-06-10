using Microsoft.Data.SqlClient;
using System.Data;

/// <summary>
/// Wraps a SqlConnection string and provides an auto-reconnecting connection.
/// Use <see cref="GetConn"/> to get an open connection before every command.
/// </summary>
public class DbHelper : IAsyncDisposable
{
    private readonly string _connStr;
    private SqlConnection? _conn;

    public DbHelper(string connStr)
    {
        _connStr = connStr;
    }

    /// <summary>Returns an open SqlConnection, reconnecting if it was dropped.</summary>
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
