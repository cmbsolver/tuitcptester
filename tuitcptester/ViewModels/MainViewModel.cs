using System.Collections.ObjectModel;
using System.Text.Json;
using tuitcptester.Logic;
using tuitcptester.Models;

namespace tuitcptester.ViewModels;

/// <summary>
/// ViewModel for the main view, managing connection instances and logs.
/// </summary>
public class MainViewModel
{
    /// <summary>
    /// The maximum number of log entries to keep in memory.
    /// </summary>
    private const int MaxLogCount = 50;

    /// <summary>
    /// Collection of TCP connection instances managed by the view.
    /// </summary>
    public ObservableCollection<TcpInstance> Instances { get; } = new();

    /// <summary>
    /// Collection of log entries displayed in the log view.
    /// </summary>
    public ObservableCollection<LogEntry> Logs { get; } = new();

    private readonly Dictionary<TcpInstance, Action<string>> _errorHandlers = new();

    /// <summary>
    /// Adds a log message to the log collection.
    /// </summary>
    /// <param name="entry">The log entry.</param>
    public void AddLog(LogEntry entry)
    {
        Logs.Insert(0, entry);
        while (Logs.Count > MaxLogCount)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    /// <summary>
    /// Formats a log entry for display and export.
    /// </summary>
    /// <param name="entry">The log entry.</param>
    /// <returns>The formatted log line.</returns>
    public static string FormatLogEntry(LogEntry entry)
    {
        return $"[{entry.Timestamp:HH:mm:ss}] [{entry.ConnectionName}] {entry.Message}";
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void ClearLogs()
    {
        Logs.Clear();
    }

    /// <summary>
    /// Adds a new connection instance and wires up its events.
    /// </summary>
    /// <param name="instance">The TCP instance to add.</param>
    public void AddInstance(TcpInstance instance)
    {
        Instances.Add(instance);
        instance.OnLog += AddLog;

        Action<string> errorHandler = (msg) =>
        {
            AddLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                ConnectionName = $"ERROR/{instance.Config.Name}",
                Message = msg
            });
        };

        _errorHandlers[instance] = errorHandler;
        instance.OnError += errorHandler;
    }

    /// <summary>
    /// Removes and disposes a connection instance.
    /// </summary>
    /// <param name="instance">The TCP instance to remove.</param>
    public void RemoveInstance(TcpInstance instance)
    {
        instance.OnLog -= AddLog;

        if (_errorHandlers.Remove(instance, out var errorHandler))
        {
            instance.OnError -= errorHandler;
        }

        instance.Stop();
        instance.Dispose();
        Instances.Remove(instance);
    }


    /// <summary>
    /// Loads connection instances from a JSON string.
    /// </summary>
    /// <param name="json">The JSON configuration string.</param>
    public void ImportConfiguration(string json)
    {
        AppConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<AppConfig>(json);
        }
        catch (Exception ex)
        {
            AddLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                ConnectionName = "CONFIG",
                Message = $"Failed to parse configuration: {ex.Message}"
            });
            return;
        }

        if (config == null)
        {
            AddLog(new LogEntry
            {
                Timestamp = DateTime.Now,
                ConnectionName = "CONFIG",
                Message = "Failed to parse configuration: empty or invalid payload."
            });
            return;
        }

        foreach (var instance in config.Connections.Select(c => new TcpInstance(c)))
        {
            AddInstance(instance);
            try
            {
                instance.Start();
            }
            catch (Exception ex)
            {
                AddLog(new LogEntry
                {
                    Timestamp = DateTime.Now,
                    ConnectionName = $"ERROR/{instance.Config.Name}",
                    Message = $"Failed to start instance: {ex.Message}"
                });
            }
        }
    }
}
