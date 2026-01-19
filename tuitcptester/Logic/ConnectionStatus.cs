namespace tuitcptester.Logic;

/// <summary>
/// Represents the current status of a TCP connection.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Not connected or listening.
    /// </summary>
    Disconnected,
    /// <summary>
    /// Successfully connected as a client.
    /// </summary>
    Connected,
    /// <summary>
    /// Successfully listening as a server.
    /// </summary>
    Listening,
    /// <summary>
    /// An error occurred during the connection or operation.
    /// </summary>
    Error
}
