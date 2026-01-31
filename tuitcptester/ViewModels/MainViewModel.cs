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
    /// Collection of log strings displayed in the log view.
    /// </summary>
    public ObservableCollection<string> Logs { get; } = new();

    /// <summary>
    /// Adds a log message to the log collection.
    /// </summary>
    /// <param name="formattedMsg">The formatted log message.</param>
    public void AddLog(string formattedMsg)
    {
        Logs.Insert(0, formattedMsg);
        while (Logs.Count > MaxLogCount)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
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
        instance.OnLog += (entry) =>
        {
            AddLog($"[{entry.Timestamp:HH:mm:ss}] [{entry.ConnectionName}] {entry.Message}");
        };
        instance.OnError += (msg) =>
        {
            AddLog($"[{DateTime.Now:HH:mm:ss}] [ERROR] [{instance.Config.Name}] {msg}");
        };
    }

    /// <summary>
    /// Removes and disposes a connection instance.
    /// </summary>
    /// <param name="instance">The TCP instance to remove.</param>
    public void RemoveInstance(TcpInstance instance)
    {
        instance.Stop();
        instance.Dispose();
        Instances.Remove(instance);
    }

    /// <summary>
    /// Saves the current configuration to a JSON string.
    /// </summary>
    /// <returns>A JSON representation of the configuration.</returns>
    public string ExportConfiguration()
    {
        var configs = Instances.Select(i => i.Config).ToList();
        return JsonSerializer.Serialize(new AppConfig { Connections = configs }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Loads connection instances from a JSON string.
    /// </summary>
    /// <param name="json">The JSON configuration string.</param>
    public void ImportConfiguration(string json)
    {
        var config = JsonSerializer.Deserialize<AppConfig>(json);
        if (config == null) return;
        foreach (var instance in config.Connections.Select(c => new TcpInstance(c)))
        {
            AddInstance(instance);
            try
            {
                instance.Start();
            }
            catch
            {
                // Errors are logged via OnError event
            }
        }
    }
}
