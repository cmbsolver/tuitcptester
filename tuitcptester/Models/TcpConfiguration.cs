namespace tuitcptester.Models;

/// <summary>
/// Configuration settings for a TCP connection.
/// </summary>
public class TcpConfiguration
{
    /// <summary>
    /// Gets or sets the display name for the connection.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type (Server or Client).
    /// </summary>
    public ConnectionType Type { get; set; }

    /// <summary>
    /// Gets or sets the host address to connect to or listen on.
    /// </summary>
    public string Host { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the port number for the connection.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the remote host for a proxy connection.
    /// </summary>
    public string? RemoteHost { get; set; }

    /// <summary>
    /// Gets or sets the remote port for a proxy connection.
    /// </summary>
    public int? RemotePort { get; set; }

    /// <summary>
    /// Gets the list of transactions to be sent automatically.
    /// </summary>
    public List<Transaction> AutoTransactions { get; } = new();

    /// <summary>
    /// Gets or sets the fixed interval in milliseconds between auto transactions.
    /// </summary>
    public int? IntervalMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum jitter in milliseconds for randomized intervals.
    /// </summary>
    public int? JitterMinMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum jitter in milliseconds for randomized intervals.
    /// </summary>
    public int? JitterMaxMs { get; set; }

    /// <summary>
    /// Gets or sets the path to a file where all communication should be dumped.
    /// </summary>
    public string? DumpFilePath { get; set; }
}
