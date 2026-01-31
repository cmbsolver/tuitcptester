using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
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
            Width = 60, Height = 21
        };
        dialog.SetScheme(this.GetScheme());
        var label = new Label { Text = "Port: ", X = 1, Y = 1 };
        var portField = new TextField { Text = "", X = Pos.Right(label), Y = 1, Width = 20 };
        
        var autoTxLabel = new Label { Text = "Auto Transactions (one per line):", X = 1, Y = 3 };
        var autoTxField = new Editor { X = 1, Y = 4, Width = Dim.Fill() - 2, Height = 5 };

        var loadFileBtn = new Button { Text = "Load from File", X = 1, Y = 9 };
        loadFileBtn.Accepting += (_,_) => {
            var fileDialog = new OpenDialog { Title = "Load Transactions" };
            _app.Run(fileDialog);
            if (fileDialog is not { Canceled: false, FilePaths.Count: > 0 }) return;
            var path = fileDialog.FilePaths[0];
            try {
                var content = File.ReadAllText(path);
                autoTxField.Text = content;
            } catch (Exception ex) {
                MessageBox.ErrorQuery(_app, "Load Error", $"Could not load file: {ex.Message}", "Ok");
            }
        };

        var intervalLabel = new Label { Text = "Interval (ms, optional):", X = 1, Y = 11 };
        var intervalField = new TextField { X = Pos.Right(intervalLabel) + 1, Y = 11, Width = 10 };

        var jitterLabel = new Label { Text = "Jitter Min/Max (ms):", X = 1, Y = 12 };
        var jitterMinField = new TextField { X = Pos.Right(jitterLabel) + 1, Y = 12, Width = 8 };
        var jitterMaxField = new TextField { X = Pos.Right(jitterMinField) + 1, Y = 12, Width = 8 };

        var dumpLabel = new Label { Text = "Dump to File:", X = 1, Y = 14 };
        var dumpField = new TextField { X = 1, Y = 15, Width = Dim.Fill() - 12 };
        var dumpBrowseBtn = new Button { Text = "Browse", X = Pos.Right(dumpField) + 1, Y = 15 };
        var payloadDumpCheckBox = new CheckBox { Text = "Include payload hex dump in logs", X = 1, Y = 17 };
        dumpBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Dump File" };
            _app.Run(saveDialog);
            if (saveDialog is { Canceled: false, Path: not null }) {
                dumpField.Text = saveDialog.Path;
            }
        };

        dialog.Add(label, portField, autoTxLabel, autoTxField, loadFileBtn, intervalLabel, intervalField, jitterLabel, jitterMinField, jitterMaxField, dumpLabel, dumpField, dumpBrowseBtn, payloadDumpCheckBox);
        
        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (_,_) =>
        {
            if (!int.TryParse(portField.Text, out var port)) return;
            var config = new TcpConfiguration { Name = $"Server:{port}", Type = ConnectionType.Server, Port = port };
            
            var lines = autoTxField.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var line in lines)
            {
                config.AutoTransactions.Add(new Transaction { Data = line, Encoding = TransactionEncoding.Ascii });
            }

            if (int.TryParse(intervalField.Text, out var interval)) config.IntervalMs = interval;
            if (int.TryParse(jitterMinField.Text, out var jMin)) config.JitterMinMs = jMin;
            if (int.TryParse(jitterMaxField.Text, out var jMax)) config.JitterMaxMs = jMax;
            config.DumpFilePath = dumpField.Text;
            config.IncludePayloadHexDump = payloadDumpCheckBox.Value == CheckState.Checked;

            var instance = new TcpInstance(config);
            try 
            {
                instance.Start();
                AddInstance(instance);
                _app.RequestStop();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery(_app, "Server Error", $"Could not start server: {ex.Message}", "Ok");
                instance.Dispose();
            }
        };
        dialog.AddButton(startBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        
        _app.Run(dialog);
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
        var autoTxField = new Editor { X = 1, Y = 6, Width = Dim.Fill() - 2, Height = 5 };

        var loadFileBtn = new Button { Text = "Load from File", X = 1, Y = 11 };
        loadFileBtn.Accepting += (_,_) => {
            var fileDialog = new OpenDialog { Title = "Load Transactions" };
            _app.Run(fileDialog);
            if (fileDialog is not { Canceled: false, FilePaths.Count: > 0 }) return;
            var path = fileDialog.FilePaths[0];
            try {
                var content = File.ReadAllText(path);
                autoTxField.Text = content;
            } catch (Exception ex) {
                MessageBox.ErrorQuery(_app, "Load Error", $"Could not load file: {ex.Message}", "Ok");
            }
        };

        var intervalLabel = new Label { Text = "Interval (ms, optional):", X = 1, Y = 13 };
        var intervalField = new TextField { X = Pos.Right(intervalLabel) + 1, Y = 13, Width = 10 };

        var jitterLabel = new Label { Text = "Jitter Min/Max (ms):", X = 1, Y = 14 };
        var jitterMinField = new TextField { X = Pos.Right(jitterLabel) + 1, Y = 14, Width = 8 };
        var jitterMaxField = new TextField { X = Pos.Right(jitterMinField) + 1, Y = 14, Width = 8 };

        var dumpLabel = new Label { Text = "Dump to File:", X = 1, Y = 16 };
        var dumpField = new TextField { X = 1, Y = 17, Width = Dim.Fill() - 12 };
        var dumpBrowseBtn = new Button { Text = "Browse", X = Pos.Right(dumpField) + 1, Y = 17 };
        var payloadDumpCheckBox = new CheckBox { Text = "Include payload hex dump in logs", X = 1, Y = 19 };
        dumpBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Dump File" };
            _app.Run(saveDialog);
            if (saveDialog is { Canceled: false, Path: not null }) {
                dumpField.Text = saveDialog.Path;
            }
        };

        var dialog = new Dialog {
            Title = "New TCP Client",
            Width = 60, Height = 25
        };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(hostLabel, hostField, portLabel, portField, autoTxLabel, autoTxField, loadFileBtn, intervalLabel, intervalField, jitterLabel, jitterMinField, jitterMaxField, dumpLabel, dumpField, dumpBrowseBtn, payloadDumpCheckBox);

        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (_,_) => {
            if (int.TryParse(portField.Text, out var port))
            {
                var config = new TcpConfiguration { 
                    Name = $"Client:{hostField.Text}:{port}", 
                    Type = ConnectionType.Client, 
                    Host = hostField.Text, 
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
                config.DumpFilePath = dumpField.Text;
                config.IncludePayloadHexDump = payloadDumpCheckBox.Value == CheckState.Checked;

                var instance = new TcpInstance(config);
                // We'll wrap the start in a try-catch. 
                // Note: For clients, since Start() spawns a thread, we should ideally 
                // check if the first connection attempt succeeds.
                try 
                {
                    instance.Start();
                    AddInstance(instance);
                    _app.RequestStop();
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery(_app, "Client Error", $"Could not initiate client: {ex.Message}", "Ok");
                    instance.Dispose();
                }
            }
        };
        dialog.AddButton(startBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        
        _app.Run(dialog);
    }

    /// <summary>
    /// Opens a dialog to create a new TCP Proxy.
    /// </summary>
    private void OnNewProxy()
    {
        var localPortLabel = new Label { Text = "Local Port:", X = 1, Y = 1 };
        var localPortField = new TextField { Text = "8080", X = 20, Y = 1, Width = 10 };

        var remoteHostLabel = new Label { Text = "Remote Host:", X = 1, Y = 2 };
        var remoteHostField = new TextField { Text = "127.0.0.1", X = 20, Y = 2, Width = 30 };

        var remotePortLabel = new Label { Text = "Remote Port:", X = 1, Y = 3 };
        var remotePortField = new TextField { Text = "80", X = 20, Y = 3, Width = 10 };
        var payloadDumpCheckBox = new CheckBox { Text = "Include payload hex dump in logs", X = 1, Y = 5 };

        var dialog = new Dialog
        {
            Title = "New TCP Proxy",
            Width = 60, Height = 12
        };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(localPortLabel, localPortField, remoteHostLabel, remoteHostField, remotePortLabel, remotePortField, payloadDumpCheckBox);

        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (_,_) =>
        {
            if (int.TryParse(localPortField.Text, out var localPort) &&
                int.TryParse(remotePortField.Text, out var remotePort))
            {
                var config = new TcpConfiguration
                {
                    Name = $"Proxy:{localPort}->{remoteHostField.Text}:{remotePort}",
                    Type = ConnectionType.Proxy,
                    Port = localPort,
                    RemoteHost = remoteHostField.Text,
                    RemotePort = remotePort,
                    IncludePayloadHexDump = payloadDumpCheckBox.Value == CheckState.Checked
                };

                var instance = new TcpInstance(config);
                try
                {
                    instance.Start();
                    AddInstance(instance);
                    _app.RequestStop();
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery(_app, "Proxy Error", $"Could not start proxy: {ex.Message}", "Ok");
                    instance.Dispose();
                }
            }
            else
            {
                MessageBox.ErrorQuery(_app, "Input Error", "Invalid port numbers.", "Ok");
            }
        };
        dialog.AddButton(startBtn);

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);

        _app.Run(dialog);
    }

    /// <summary>
    /// Removes and disposes the selected connection.
    /// </summary>
    private void OnDisposeConnection()
    {
        if (_selectedInstance == null)
        {
            MessageBox.ErrorQuery(_app, "Remove Connection", "No connection selected to remove.", "Ok");
            return;
        }

        var result = MessageBox.Query(_app, "Remove Connection", $"Are you sure you want to remove '{_selectedInstance.Config.Name}'?", "Yes", "No");
        if (result != 0) return;
        _viewModel.RemoveInstance(_selectedInstance);
        _selectedInstance = null;
        UpdateDetails();
    }
    
    /// <summary>
    /// Adds a new <see cref="TcpInstance"/> to the application's view model.
    /// </summary>
    /// <param name="instance">The TCP instance to add.</param>
    private void AddInstance(TcpInstance instance)
    {
        _viewModel.AddInstance(instance);
    }
}
