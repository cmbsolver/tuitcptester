using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Defines the interface for a TCP connection (client, server, or proxy).
/// </summary>
public interface ITcpConnection : IDisposable
{
    /// <summary>
    /// Event raised when a new log entry is available.
    /// </summary>
    event Action<string>? OnLog;

    /// <summary>
    /// Event raised when the connection status changes.
    /// </summary>
    event Action<ConnectionStatus>? OnStatusChanged;

    /// <summary>
    /// Event raised when an error occurs.
    /// </summary>
    event Action<string>? OnError;

    /// <summary>
    /// Starts the connection.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the connection.
    /// </summary>
    void Stop();

    /// <summary>
    /// Sends a transaction over the connection.
    /// </summary>
    /// <param name="tx">The transaction to send.</param>
    void Send(Transaction tx);

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    ConnectionStatus Status { get; }
}
