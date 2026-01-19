namespace tuitcptester.Models;

/// <summary>
/// Specifies the type of TCP connection.
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Acts as a TCP server.
    /// </summary>
    Server,
    /// <summary>
    /// Acts as a TCP client.
    /// </summary>
    Client
}

/// <summary>
/// Specifies the encoding used for transactions.
/// </summary>
public enum TransactionEncoding
{
    /// <summary>
    /// Plain text ASCII encoding.
    /// </summary>
    Ascii,
    /// <summary>
    /// Hexadecimal string representation.
    /// </summary>
    Hex,
    /// <summary>
    /// Binary data (typically represented as Base64 in config).
    /// </summary>
    Binary
}

/// <summary>
/// Represents a single data transaction.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the data to be sent.
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encoding used for the data.
    /// </summary>
    public TransactionEncoding Encoding { get; set; } = TransactionEncoding.Ascii;
}

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
}

/// <summary>
/// Represents the root application configuration.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Gets or sets the list of configured TCP connections.
    /// </summary>
    public List<TcpConfiguration> Connections { get; init; } = new();
}
