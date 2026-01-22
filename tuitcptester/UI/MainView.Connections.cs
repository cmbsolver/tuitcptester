using Terminal.Gui;
using tuitcptester.Logic;
using tuitcptester.Models;

namespace tuitcptester.UI;

public sealed partial class MainView
{
    /// <summary>
    /// Opens a dialog to create and start a new TCP server.
    /// </summary>
    private void OnNewServer()
    {
        var dialog = new Dialog {
            Title = "New TCP Server",
            Width = 60, Height = 21,
            ColorScheme = ColorScheme
        };
        var label = new Label { Text = "Port: ", X = 1, Y = 1 };
        var portField = new TextField { Text = "", X = Pos.Right(label), Y = 1, Width = 20 };
        
        var autoTxLabel = new Label { Text = "Auto Transactions (one per line):", X = 1, Y = 3 };
        var autoTxField = new TextView { X = 1, Y = 4, Width = Dim.Fill()! - 2, Height = 5 };

        var loadFileBtn = new Button { Text = "Load from File", X = 1, Y = 9 };
        loadFileBtn.Accepting += (s, e) => {
            var fileDialog = new OpenDialog { Title = "Load Transactions" };
            Application.Run(fileDialog);
            if (!fileDialog.Canceled && fileDialog.FilePaths.Count > 0)
            {
                var path = fileDialog.FilePaths[0];
                try {
                    var content = File.ReadAllText(path);
                    autoTxField.Text = content;
                } catch (Exception ex) {
                    MessageBox.ErrorQuery("Load Error", $"Could not load file: {ex.Message}", "Ok");
                }
            }
        };

        var intervalLabel = new Label { Text = "Interval (ms, optional):", X = 1, Y = 11 };
        var intervalField = new TextField { X = Pos.Right(intervalLabel) + 1, Y = 11, Width = 10 };

        var jitterLabel = new Label { Text = "Jitter Min/Max (ms):", X = 1, Y = 12 };
        var jitterMinField = new TextField { X = Pos.Right(jitterLabel) + 1, Y = 12, Width = 8 };
        var jitterMaxField = new TextField { X = Pos.Right(jitterMinField) + 1, Y = 12, Width = 8 };

        var dumpLabel = new Label { Text = "Dump to File:", X = 1, Y = 14 };
        var dumpField = new TextField { X = 1, Y = 15, Width = Dim.Fill()! - 12 };
        var dumpBrowseBtn = new Button { Text = "Browse", X = Pos.Right(dumpField) + 1, Y = 15 };
        dumpBrowseBtn.Accepting += (s, e) => {
            var saveDialog = new SaveDialog { Title = "Select Dump File" };
            Application.Run(saveDialog);
            if (!saveDialog.Canceled && saveDialog.Path != null) {
                dumpField.Text = saveDialog.Path.ToString();
            }
        };

        dialog.Add(label, portField, autoTxLabel, autoTxField, loadFileBtn, intervalLabel, intervalField, jitterLabel, jitterMinField, jitterMaxField, dumpLabel, dumpField, dumpBrowseBtn);
        
        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (s, e) =>
        {
            if (!int.TryParse(portField.Text, out var port)) return;
            var config = new TcpConfiguration { Name = $"Server:{port}", Type = ConnectionType.Server, Port = port };
            
            var lines = autoTxField.Text.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                config.AutoTransactions.Add(new Transaction { Data = line, Encoding = TransactionEncoding.Ascii });
            }

            if (int.TryParse(intervalField.Text, out int interval)) config.IntervalMs = interval;
            if (int.TryParse(jitterMinField.Text, out int jMin)) config.JitterMinMs = jMin;
            if (int.TryParse(jitterMaxField.Text, out int jMax)) config.JitterMaxMs = jMax;
            config.DumpFilePath = dumpField.Text.ToString();

            var instance = new TcpInstance(config);
            try 
            {
                instance.Start();
                AddInstance(instance);
                Application.RequestStop();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Server Error", $"Could not start server: {ex.Message}", "Ok");
                instance.Dispose();
            }
        };
        dialog.AddButton(startBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(cancelBtn);
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Opens a dialog to create and start a new TCP client.
    /// </summary>
    private void OnNewClient()
    {
        var hostLabel = new Label { Text = "Host: ", X = 1, Y = 1 };
        var hostField = new TextField { Text = "127.0.0.1", X = Pos.Right(hostLabel), Y = 1, Width = 30 };
        var portLabel = new Label { Text = "Port: ", X = 1, Y = 3 };
        var portField = new TextField { Text = "", X = Pos.Right(portLabel), Y = 3, Width = 10 };

        var autoTxLabel = new Label { Text = "Auto Transactions (one per line):", X = 1, Y = 5 };
        var autoTxField = new TextView { X = 1, Y = 6, Width = Dim.Fill()! - 2, Height = 5 };

        var loadFileBtn = new Button { Text = "Load from File", X = 1, Y = 11 };
        loadFileBtn.Accepting += (s, e) => {
            var fileDialog = new OpenDialog { Title = "Load Transactions" };
            Application.Run(fileDialog);
            if (!fileDialog.Canceled && fileDialog.FilePaths.Count > 0)
            {
                var path = fileDialog.FilePaths[0];
                try {
                    var content = File.ReadAllText(path);
                    autoTxField.Text = content;
                } catch (Exception ex) {
                    MessageBox.ErrorQuery("Load Error", $"Could not load file: {ex.Message}", "Ok");
                }
            }
        };

        var intervalLabel = new Label { Text = "Interval (ms, optional):", X = 1, Y = 13 };
        var intervalField = new TextField { X = Pos.Right(intervalLabel) + 1, Y = 13, Width = 10 };

        var jitterLabel = new Label { Text = "Jitter Min/Max (ms):", X = 1, Y = 14 };
        var jitterMinField = new TextField { X = Pos.Right(jitterLabel) + 1, Y = 14, Width = 8 };
        var jitterMaxField = new TextField { X = Pos.Right(jitterMinField) + 1, Y = 14, Width = 8 };

        var dumpLabel = new Label { Text = "Dump to File:", X = 1, Y = 16 };
        var dumpField = new TextField { X = 1, Y = 17, Width = Dim.Fill()! - 12 };
        var dumpBrowseBtn = new Button { Text = "Browse", X = Pos.Right(dumpField) + 1, Y = 17 };
        dumpBrowseBtn.Accepting += (s, e) => {
            var saveDialog = new SaveDialog { Title = "Select Dump File" };
            Application.Run(saveDialog);
            if (!saveDialog.Canceled && saveDialog.Path != null) {
                dumpField.Text = saveDialog.Path.ToString();
            }
        };

        var dialog = new Dialog {
            Title = "New TCP Client",
            Width = 60, Height = 23,
            ColorScheme = ColorScheme
        };
        dialog.Add(hostLabel, hostField, portLabel, portField, autoTxLabel, autoTxField, loadFileBtn, intervalLabel, intervalField, jitterLabel, jitterMinField, jitterMaxField, dumpLabel, dumpField, dumpBrowseBtn);

        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (s, e) => {
            if (int.TryParse(portField.Text, out int port))
            {
                var config = new TcpConfiguration { 
                    Name = $"Client:{hostField.Text}:{port}", 
                    Type = ConnectionType.Client, 
                    Host = hostField.Text!, 
                    Port = port 
                };
                
                var lines = autoTxField.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var line in lines)
                {
                    config.AutoTransactions.Add(new Transaction { Data = line, Encoding = TransactionEncoding.Ascii });
                }

                if (int.TryParse(intervalField.Text, out int interval)) config.IntervalMs = interval;
                if (int.TryParse(jitterMinField.Text, out int jMin)) config.JitterMinMs = jMin;
                if (int.TryParse(jitterMaxField.Text, out int jMax)) config.JitterMaxMs = jMax;
                config.DumpFilePath = dumpField.Text.ToString();

                var instance = new TcpInstance(config);
                // We'll wrap the start in a try-catch. 
                // Note: For clients, since Start() spawns a thread, we should ideally 
                // check if the first connection attempt succeeds.
                try 
                {
                    instance.Start();
                    AddInstance(instance);
                    Application.RequestStop();
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Client Error", $"Could not initiate client: {ex.Message}", "Ok");
                    instance.Dispose();
                }
            }
        };
        dialog.AddButton(startBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(cancelBtn);
        
        Application.Run(dialog);
    }

    /// <summary>
    /// Removes and disposes the selected connection.
    /// </summary>
    private void OnDisposeConnection()
    {
        if (_selectedInstance == null)
        {
            MessageBox.ErrorQuery("Remove Connection", "No connection selected to remove.", "Ok");
            return;
        }

        var result = MessageBox.Query("Remove Connection", $"Are you sure you want to remove '{_selectedInstance.Config.Name}'?", "Yes", "No");
        if (result == 0)
        {
            _selectedInstance.Dispose();
            _instances.Remove(_selectedInstance);
            _selectedInstance = null;
            _connectionList.SetSource(_instances); // Refresh the list view
            UpdateDetails();
        }
    }
    
    /// <summary>
    /// Adds a new <see cref="TcpInstance"/> to the collection and sets up logging and event handlers.
    /// </summary>
    /// <param name="instance">The TCP instance to add.</param>
    private void AddInstance(TcpInstance instance)
    {
        _instances.Add(instance);
        instance.OnLog += (entry) => {
            Application.Invoke(() => {
                _logs.Insert(0, $"[{entry.Timestamp:HH:mm:ss}] [{entry.ConnectionName}] {entry.Message}");
                if (_logs.Count > 50) _logs.RemoveAt(50);
            });
        };
        instance.OnStatusChanged += () => {
            Application.Invoke(UpdateDetails);
        };
        instance.OnError += (msg) => {
            Application.Invoke(() => {
                MessageBox.ErrorQuery("Connection Error", msg, "Ok");
            });
        };
    }
}
