namespace tuitcptester.Models;

/// <summary>
/// Configuration settings for a TCP connection.
/// </summary>
public class TcpConfiguration
{
    /// <summary>
    /// Gets or sets the display name for the connection.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type (Server or Client).
    /// </summary>
    public ConnectionType Type { get; init; }

    /// <summary>
    /// Gets or sets the host address to connect to or listen on.
    /// </summary>
    public string Host { get; init; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the port number for the connection.
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// Gets or sets the remote host for a proxy connection.
    /// </summary>
    public string? RemoteHost { get; init; }

    /// <summary>
    /// Gets or sets the remote port for a proxy connection.
    /// </summary>
    public int? RemotePort { get; init; }

    /// <summary>
    /// Gets the list of transactions to be sent automatically.
    /// </summary>
    public List<Transaction> AutoTransactions { get; } = [];

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

    /// <summary>
    /// Gets or sets a value indicating whether payload logs should include full hex dumps.
    /// </summary>
    public bool IncludePayloadHexDump { get; set; }
}
