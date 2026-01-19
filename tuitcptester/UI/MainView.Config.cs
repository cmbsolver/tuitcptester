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
        var dialog = new SaveDialog
        {
            Title = "Save Configuration",
            OpenMode = OpenMode.File
        };
        dialog.Path = "config.json";

        Application.Run(dialog);

        if (dialog.Path == null || dialog.Canceled) return;
        var path = dialog.Path.ToString();
        if (string.IsNullOrEmpty(path)) return;

        var configs = _instances.Select(i => i.Config).ToList();
        var json = JsonSerializer.Serialize(new AppConfig { Connections = configs }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        MessageBox.Query("Save", $"Configuration saved to {Path.GetFileName(path)}", "Ok");
    }

    /// <summary>
    /// Loads a configuration from the specified file path.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    public void LoadConfig(string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json);
        if (config == null) return;
        foreach (var instance in config.Connections.Select(c => new TcpInstance(c)))
        {
            AddInstance(instance);
            instance.Start();
        }
    }

    /// <summary>
    /// Opens a dialog to load a configuration file.
    /// </summary>
    private void OnLoadConfig()
    {
        using var dialog = new OpenDialog();
        dialog.Title = "Load Configuration";
        dialog.AllowsMultipleSelection = false;
        dialog.OpenMode = OpenMode.File;

        Application.Run(dialog);

        if (dialog.Canceled || dialog.FilePaths.Count <= 0) return;
        var path = dialog.FilePaths[0];
        if (!File.Exists(path)) return;
        try
        {
            LoadConfig(path);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to load config: {ex.Message}", "Ok");
        }
    }
}
