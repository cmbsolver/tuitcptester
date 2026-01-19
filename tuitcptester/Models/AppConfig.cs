namespace tuitcptester.Models;

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
