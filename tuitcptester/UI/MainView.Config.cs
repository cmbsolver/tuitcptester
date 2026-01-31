using Terminal.Gui.Views;
using System.Text.Json;
using tuitcptester.Models;
using tuitcptester.ViewModels;

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

        _app.Run(dialog);

        if (dialog.Canceled) return;
        var path = dialog.Path;
        if (string.IsNullOrEmpty(path)) return;

        var configs = _viewModel.Instances.Select(i => i.Config).ToList();
        var json = JsonSerializer.Serialize(new AppConfig { Connections = configs }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        MessageBox.Query(_app, "Save", $"Configuration saved to {Path.GetFileName(path)}", "Ok");
    }

    /// <summary>
    /// Loads a configuration from the specified file path.
    /// </summary>
    /// <param name="path">The path to the configuration file.</param>
    public void LoadConfig(string path)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        _viewModel.ImportConfiguration(json);
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

        _app.Run(dialog);

        if (dialog.Canceled || dialog.FilePaths.Count <= 0) return;
        var path = dialog.FilePaths[0];
        if (!File.Exists(path)) return;
        try
        {
            LoadConfig(path);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(_app, "Error", $"Failed to load config: {ex.Message}", "Ok");
        }
    }

    /// <summary>
    /// Exports the current logs to a text file.
    /// </summary>
    private void OnExportLogs()
    {
        if (_viewModel.Logs.Count == 0)
        {
            MessageBox.Query(_app, "Export", "No logs to export.", "Ok");
            return;
        }

        var dialog = new SaveDialog
        {
            Title = "Export Logs",
            OpenMode = OpenMode.File
        };
        dialog.Path = "logs.txt";

        _app.Run(dialog);

        if (dialog.Canceled) return;
        var path = dialog.Path;
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            File.WriteAllLines(path, _viewModel.Logs.Select(MainViewModel.FormatLogEntry));
            MessageBox.Query(_app, "Export", $"Logs exported to {Path.GetFileName(path)}", "Ok");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(_app, "Export Error", ex.Message, "Ok");
        }
    }
}
