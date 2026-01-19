using Terminal.Gui;
using System.Text.Json;
using tuitcptester.Models;
using tuitcptester.Logic;

namespace tuitcptester.UI;

public sealed partial class MainView
{
    /// <summary>
    /// Saves the current configuration to a file.
    /// </summary>
    private void OnSaveConfig()
    {
        var configs = _instances.Select(i => i.Config).ToList();
        var json = JsonSerializer.Serialize(new AppConfig { Connections = configs });
        File.WriteAllText("config.json", json);
        MessageBox.Query("Save", "Configuration saved to config.json", "Ok");
    }

    /// <summary>
    /// Loads a configuration from the specified file path.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    public void LoadConfig(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<AppConfig>(json);
            if (config != null)
            {
                foreach (var c in config.Connections)
                {
                    var instance = new TcpInstance(c);
                    AddInstance(instance);
                    instance.Start();
                }
            }
        }
    }

    /// <summary>
    /// Opens a dialog to load a configuration file.
    /// </summary>
    private void OnLoadConfig()
    {
        // Simple file prompt for demo purposes, could be improved with a file dialog
        LoadConfig("config.json");
    }
}
