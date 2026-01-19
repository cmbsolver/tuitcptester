namespace tuitcptester.Logic;

/// <summary>
/// Represents a log message with metadata.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the time when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the connection that generated this log.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;
}
