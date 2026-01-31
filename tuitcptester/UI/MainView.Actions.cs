using System.Collections.ObjectModel;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using tuitcptester.Logic;
using tuitcptester.Models;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace tuitcptester.UI;

public sealed partial class MainView
{
    /// <summary>
    /// Opens a dialog to manually send a message over the selected connection.
    /// </summary>
    private void OnSendManual()
    {
        if (!_viewModel.Instances.Any())
        {
            MessageBox.ErrorQuery(_app, "Send Message", "No connections exist. Please create a server or client first.",
                "Ok");
            return;
        }

        if (_selectedInstance == null)
        {
            MessageBox.ErrorQuery(_app, "Send Message", "Please select a connection from the list.", "Ok");
            return;
        }

        if (_selectedInstance.Status != ConnectionStatus.Connected &&
            _selectedInstance.Config.Type == ConnectionType.Client)
        {
            MessageBox.ErrorQuery(_app, "Send Message", "Client is not connected.", "Ok");
            return;
        }

        var dataLabel = new Label { Text = "Data (Hex/Base64/ASCII):", X = 1, Y = 1 };
        var dataField = new TextField { Text = "", X = 1, Y = 2, Width = Dim.Fill() - 2 };

        var encodingLabel = new Label { Text = "Encoding:", X = 1, Y = 4 };
        var encodingGroup = new OptionSelector
        {
            X = 1, Y = 5,
            Labels = ["ASCII", "Hex", "Binary (Base64)"]
        };

        var returnCheckbox = new CheckBox { Text = "Append \\r (Return)", X = 1, Y = 8 };
        var newlineCheckbox = new CheckBox { Text = "Append \\n (Newline)", X = 1, Y = 9 };

        var hintLabel = new Label { Text = "Press ESC to Cancel", X = 1, Y = 11 };
        hintLabel.SetScheme(this.GetScheme());

        var dialog = new Dialog { Title = "Manual Send", Width = 60, Height = 16 };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(dataLabel, dataField, encodingLabel, encodingGroup, returnCheckbox, newlineCheckbox, hintLabel);

        var sendBtn = new Button { Text = "Send", IsDefault = true };

        bool sendClicked = false;

        sendBtn.Accepting += (_,_) =>
        {
            sendClicked = true;
            _app.RequestStop();
        };

        dialog.AddButton(sendBtn);

        // Run the dialog modally
        _app.Run(dialog);

        // If the user closed the dialog via Escape or the Cancel button, 
        // sendClicked will be false.
        if (sendClicked)
        {
            var tx = new Transaction
            {
                Data = dataField.Text,
                Encoding = (TransactionEncoding)encodingGroup.Value!,
                AppendReturn = returnCheckbox.Value == CheckState.Checked,
                AppendNewline = newlineCheckbox.Value == CheckState.Checked
            };

            _selectedInstance.SendManual(tx);
        }
    }

    /// <summary>
    /// Displays a dialog listing all IP addresses for the machine.
    /// </summary>
    private void OnListIPs()
    {
        var tmpAddresses = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Select(ua => $"{ua.Address} ({ua.Address.AddressFamily})")
            .ToList();

        ObservableCollection<string> ipAddresses = new ObservableCollection<string>(tmpAddresses);

        if (!ipAddresses.Any())
        {
            ipAddresses.Add("No active IP addresses found.");
        }

        var dialog = new Dialog { Title = "Machine IP Addresses", Width = 60, Height = 12 };
        dialog.SetScheme(GetScheme());
        var list = new ListView
        {
            Source = new ListWrapper<string>(ipAddresses),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };
        dialog.Add(list);

        var okBtn = new Button { Text = "Ok", IsDefault = true };
        okBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(okBtn);

        _app.Run(dialog);
    }

    /// <summary>
    /// Starts the selected connection.
    /// </summary>
    private void OnStartConnection()
    {
        if (_selectedInstance == null) return;
        if (_selectedInstance.Status != ConnectionStatus.Disconnected &&
            _selectedInstance.Status != ConnectionStatus.Error) return;

        try
        {
            _selectedInstance.Start();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(_app, "Start Error", ex.Message, "Ok");
        }
    }

    /// <summary>
    /// Stops the selected connection.
    /// </summary>
    private void OnStopConnection()
    {
        _selectedInstance?.Stop();
    }

    /// <summary>
    /// Opens a dialog to ping an IP address.
    /// </summary>
    private void OnPing()
    {
        var hostField = new TextField { Text = "127.0.0.1", X = 13, Y = 1, Width = 30 };
        var logFileLabel = new Label { Text = "Log to File:", X = 1, Y = 3 };
        var logFileField = new TextField { Text = "", X = 1, Y = 4, Width = Dim.Fill() - 12 };
        var logFileBrowseBtn = new Button { Text = "Browse", X = Pos.Right(logFileField) + 1, Y = 4 };
        logFileBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Log File" };
            _app.Run(saveDialog);
            if (!saveDialog.Canceled) {
                logFileField.Text = saveDialog.Path;
            }
        };

        var dialog = new Dialog { Title = "Ping IP", Width = 50, Height = 10 };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(
            new Label { Text = "IP Address:", X = 1, Y = 1 }, hostField,
            logFileLabel, logFileField, logFileBrowseBtn
        );

        var pingBtn = new Button { Text = "Ping", IsDefault = true };
        pingBtn.Accepting += (_,_) =>
        {
            var host = hostField.Text;
            var logFilePath = logFileField.Text;
            Task.Run(() =>
            {
                try
                {
                    var ping = new System.Net.NetworkInformation.Ping();
                    var results = new List<string>();
                    for (int i = 0; i < 10; i++)
                    {
                        var reply = ping.Send(host);
                        var msg = $"#{i + 1}: Status: {reply.Status}, Time: {reply.RoundtripTime}ms";
                        results.Add(msg);
                        
                        if (!string.IsNullOrWhiteSpace(logFilePath))
                        {
                            try
                            {
                                File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Ping] {host} {msg}{Environment.NewLine}");
                            }
                            catch { /* ignore */ }
                        }
                    }

                    string summary = string.Join("\n", results);
                    _app.Invoke(() => MessageBox.Query(_app, "Ping Results", summary, "Ok"));
                }
                catch (Exception ex)
                {
                    _app.Invoke(() => MessageBox.ErrorQuery(_app, "Ping Error", ex.Message, "Ok"));
                }
            });
            _app.RequestStop();
        };
        dialog.AddButton(pingBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        _app.Run(dialog);
    }

    /// <summary>
    /// Opens a dialog to scan a range of ports on a host.
    /// </summary>
    private void OnPortScan()
    {
        var hostField = new TextField { Text = "127.0.0.1", X = 13, Y = 1, Width = 30 };
        var startPortField = new TextField { Text = "1", X = 13, Y = 2, Width = 10 };
        var endPortField = new TextField { Text = "65535", X = 13, Y = 3, Width = 10 };

        var dialog = new Dialog { Title = "Port Scan", Width = 50, Height = 17 };
        dialog.SetScheme(GetScheme());

        var logFileLabel = new Label { Text = "Log to File:", X = 1, Y = 5 };
        var logFileField = new TextField { Text = "", X = 1, Y = 6, Width = Dim.Fill() - 12 };
        var logFileBrowseBtn = new Button { Text = "Browse", X = Pos.Right(logFileField) + 1, Y = 6 };
        logFileBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Log File" };
            _app.Run(saveDialog);
            if (saveDialog is { Canceled: false, Path: not null }) {
                logFileField.Text = saveDialog.Path;
            }
        };

        dialog.Add(
            new Label { Text = "Host:", X = 1, Y = 1 }, hostField,
            new Label { Text = "Start Port:", X = 1, Y = 2 }, startPortField,
            new Label { Text = "End Port:", X = 1, Y = 3 }, endPortField,
            logFileLabel, logFileField, logFileBrowseBtn
        );

        var scanBtn = new Button { Text = "Scan", IsDefault = true };
        scanBtn.Accepting += (s, e) =>
        {
            string host = hostField.Text;
            string logFilePath = logFileField.Text;
            if (!int.TryParse(startPortField.Text, out int startPort) ||
                !int.TryParse(endPortField.Text, out int endPort))
            {
                MessageBox.ErrorQuery(_app, "Input Error", "Invalid port range.", "Ok");
                return;
            }

            if (startPort > endPort || startPort < 1 || endPort > 65535)
            {
                MessageBox.ErrorQuery(_app, "Input Error", "Ports must be between 1 and 65535, and Start <= End.", "Ok");
                return;
            }


            _ = Task.Run(async () =>
            {
                try
                {
                    void Log(string msg)
                    {
                        var timestamp = DateTime.Now;
                        _app.Invoke(() => _viewModel.AddLog(new LogEntry
                        {
                            Timestamp = timestamp,
                            ConnectionName = "Scanner",
                            Message = msg
                        }));

                        if (!string.IsNullOrWhiteSpace(logFilePath))
                        {
                            try
                            {
                                File.AppendAllText(logFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] [Scanner] {msg}{Environment.NewLine}");
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }

                    var results = await PortScanner.ScanRangeAsync(host, startPort, endPort);
                    var openPorts = results.Where(r => r.IsOpen).Select(r => r.Port).ToList();

                    foreach (var port in openPorts)
                    {
                        var description = PortScanner.GetPortDescription(port);
                        Log($"Open port {port} ({description}) found on {host}");
                    }

                    if (openPorts.Count != 0)
                    {
                        _app.Invoke(() => MessageBox.Query(_app, "Scan Results", $"Open ports on {host}:\n{string.Join(", ", openPorts)}\n\nResults also logged to the log area.", "Ok"));
                    }
                    else
                    {
                        Log($"No open ports found on {host} in range {startPort}-{endPort}");
                        _app.Invoke(() => MessageBox.Query(_app, "Scan Results", $"No open ports found on {host} in range {startPort}-{endPort}.", "Ok"));
                    }
                }
                catch (Exception ex)
                {
                    _app.Invoke(() => MessageBox.ErrorQuery(_app, "Scan Error", ex.Message, "Ok"));
                }
            });
            _app.RequestStop();
        };

        dialog.AddButton(scanBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        _app.Run(dialog);
    }


    /// <summary>
    /// Opens a dialog to scan a range of ports and send specified data to each open port.
    /// </summary>
    private void OnPortTransactionScan()
    {
        var hostField = new TextField { Text = "127.0.0.1", X = 15, Y = 1, Width = 30 };
        var startPortField = new TextField { Text = "1", X = 15, Y = 2, Width = 10 };
        var endPortField = new TextField { Text = "65535", X = 15, Y = 3, Width = 10 };
        
        var dataLabel = new Label { Text = "Data (Hex/Base64/ASCII):", X = 1, Y = 5 };
        var dataField = new TextField { Text = "", X = 1, Y = 6, Width = Dim.Fill() - 2 };

        var encodingLabel = new Label { Text = "Encoding:", X = 1, Y = 8 };
        var encodingGroup = new OptionSelector
        {
            X = 1, Y = 9,
            Labels = ["ASCII", "Hex", "Binary (Base64)"]
        };

        var returnCheckbox = new CheckBox { Text = "Append \\r (Return)", X = 1, Y = 12 };
        var newlineCheckbox = new CheckBox { Text = "Append \\n (Newline)", X = 1, Y = 13 };

        var logFileLabel = new Label { Text = "Log to File:", X = 1, Y = 15 };
        var logFileField = new TextField { Text = "", X = 1, Y = 16, Width = Dim.Fill() - 12 };
        var logFileBrowseBtn = new Button { Text = "Browse", X = Pos.Right(logFileField) + 1, Y = 16 };
        logFileBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Log File" };
            _app.Run(saveDialog);
            if (saveDialog is { Canceled: false, Path: not null }) {
                logFileField.Text = saveDialog.Path;
            }
        };

        var dialog = new Dialog { Title = "Port Transaction Scan", Width = 60, Height = 21 };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(
            new Label { Text = "Host:", X = 1, Y = 1 }, hostField,
            new Label { Text = "Start Port:", X = 1, Y = 2 }, startPortField,
            new Label { Text = "End Port:", X = 1, Y = 3 }, endPortField,
            dataLabel, dataField, encodingLabel, encodingGroup, returnCheckbox, newlineCheckbox,
            logFileLabel, logFileField, logFileBrowseBtn
        );

        var runBtn = new Button { Text = "Run", IsDefault = true };
        runBtn.Accepting += (s, e) =>
        {
            string host = hostField.Text;
            string logFilePath = logFileField.Text;
            if (!int.TryParse(startPortField.Text, out int startPort) ||
                !int.TryParse(endPortField.Text, out int endPort))
            {
                MessageBox.ErrorQuery(_app, "Input Error", "Invalid port range.", "Ok");
                return;
            }

            var tx = new Transaction
            {
                Data = dataField.Text,
                Encoding = (TransactionEncoding)encodingGroup.Value!,
                AppendReturn = returnCheckbox.Value == CheckState.Checked,
                AppendNewline = newlineCheckbox.Value == CheckState.Checked
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    void Log(string msg)
                    {
                        var timestamp = DateTime.Now;
                        _app.Invoke(() => _viewModel.AddLog(new LogEntry
                        {
                            Timestamp = timestamp,
                            ConnectionName = "PortTxScan",
                            Message = msg
                        }));
                        if (!string.IsNullOrWhiteSpace(logFilePath))
                        {
                            try
                            {
                                File.AppendAllText(logFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
                            }
                            catch
                            {
                                // Ignore file write errors for now
                            }
                        }
                    }

                    Log($"Starting Port Transaction Scan on {host} ({startPort}-{endPort})...");
                    
                    for (int port = startPort; port <= endPort; port++)
                    {
                        int currentPort = port;
                        try
                        {
                            using var client = new System.Net.Sockets.TcpClient();
                            var connectTask = client.ConnectAsync(host, currentPort);
                            var delayTask = Task.Delay(200); // 200ms timeout like in PortScanner
                            
                            var completedTask = await Task.WhenAny(connectTask, delayTask);
                            if (completedTask == connectTask && client.Connected)
                            {
                                Log($"Port {currentPort} is OPEN. Sending data...");
                                
                                using var stream = client.GetStream();
                                stream.ReadTimeout = 1000;
                                stream.WriteTimeout = 1000;

                                // Prepare data
                                var dataToSend = tx.Data;
                                if (tx.AppendReturn) dataToSend += "\r";
                                if (tx.AppendNewline) dataToSend += "\n";

                                byte[] buffer;
                                switch (tx.Encoding)
                                {
                                    case TransactionEncoding.Ascii:
                                        buffer = System.Text.Encoding.ASCII.GetBytes(dataToSend);
                                        break;
                                    case TransactionEncoding.Hex:
                                        buffer = DataUtils.HexToBytes(dataToSend);
                                        break;
                                    case TransactionEncoding.Binary:
                                        buffer = Convert.FromBase64String(dataToSend);
                                        break;
                                    default:
                                        buffer = System.Text.Encoding.ASCII.GetBytes(dataToSend);
                                        break;
                                }

                                Log($"Port {currentPort} Sending ({tx.Encoding}) {buffer.Length} bytes:\n{DataUtils.ToHexDump(buffer, 0, buffer.Length)}");
                                await stream.WriteAsync(buffer, 0, buffer.Length);

                                // Read response
                                byte[] readBuffer = new byte[4096];
                                var readTask = stream.ReadAsync(readBuffer, 0, readBuffer.Length);
                                var readDelayTask = Task.Delay(500); // Wait up to 500ms for response
                                
                                var readCompletedTask = await Task.WhenAny(readTask, readDelayTask);
                                if (readCompletedTask == readTask)
                                {
                                    var bytesRead = await readTask;
                                    if (bytesRead > 0)
                                    {
                                        var responseHex = DataUtils.ToHexString(readBuffer, 0, bytesRead);
                                        var responseAscii = System.Text.Encoding.ASCII.GetString(readBuffer, 0, bytesRead)
                                            .Replace("\r", "\\r").Replace("\n", "\\n");
                                        Log($"Port {currentPort} Response (ASCII): {responseAscii}");
                                        Log($"Port {currentPort} Response (Hex): {responseHex}");
                                    }
                                    else
                                    {
                                        Log($"Port {currentPort}: No response data received.");
                                    }
                                }
                                else
                                {
                                    Log($"Port {currentPort}: Response timeout.");
                                }
                            }
                            else
                            {
                                Log($"Port {currentPort} is CLOSED or Connection timed out.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Port {currentPort} Error: {ex.Message}");
                        }
                    }
                    
                    Log($"Port Transaction Scan on {host} completed.");
                }
                catch (Exception ex)
                {
                    _app.Invoke(() => MessageBox.ErrorQuery(_app, "Scan Error", ex.Message, "Ok"));
                }
            });
            _app.RequestStop();
        };

        dialog.AddButton(runBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        _app.Run(dialog);
    }


    /// <summary>
    /// Opens a dialog to generate and send custom packets.
    /// </summary>
    private void OnPacketGenerator()
    {
        var hostField = new TextField { Text = "127.0.0.1", X = 15, Y = 1, Width = 30 };
        var portField = new TextField { Text = "80", X = 15, Y = 2, Width = 10 };
        var hexField = new TextField { Text = "48454c4c4f", X = 15, Y = 3, Width = Dim.Fill() - 2 };
        var iterationsField = new TextField { Text = "1", X = 15, Y = 4, Width = 10 };
        var delayField = new TextField { Text = "100", X = 15, Y = 5, Width = 10 };

        var logFileLabel = new Label { Text = "Log to File:", X = 1, Y = 7 };
        var logFileField = new TextField { Text = "", X = 1, Y = 8, Width = Dim.Fill() - 12 };
        var logFileBrowseBtn = new Button { Text = "Browse", X = Pos.Right(logFileField) + 1, Y = 8 };
        logFileBrowseBtn.Accepting += (_,_) => {
            var saveDialog = new SaveDialog { Title = "Select Log File" };
            _app.Run(saveDialog);
            if (saveDialog is { Canceled: false, Path: not null }) {
                logFileField.Text = saveDialog.Path;
            }
        };

        var dialog = new Dialog { Title = "Packet Generator", Width = 60, Height = 13 };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(
            new Label { Text = "Host:", X = 1, Y = 1 }, hostField,
            new Label { Text = "Port:", X = 1, Y = 2 }, portField,
            new Label { Text = "Hex Data:", X = 1, Y = 3 }, hexField,
            new Label { Text = "Iterations:", X = 1, Y = 4 }, iterationsField,
            new Label { Text = "Delay (ms):", X = 1, Y = 5 }, delayField,
            logFileLabel, logFileField, logFileBrowseBtn
        );

        var runBtn = new Button { Text = "Run", IsDefault = true };
        runBtn.Accepting += (s, e) =>
        {
            var host = hostField.Text;
            var hex = hexField.Text;
            var logFilePath = logFileField.Text;
            if (!int.TryParse(portField.Text, out int port) ||
                !int.TryParse(iterationsField.Text, out int iterations) ||
                !int.TryParse(delayField.Text, out int delay))
            {
                MessageBox.ErrorQuery(_app, "Input Error", "Invalid port, iterations, or delay.", "Ok");
                return;
            }

            _ = Task.Run(async () =>
            {
                await PacketGenerator.RunAsync(host, port, hex, iterations, delay, (msg) =>
                {
                    var timestamp = DateTime.Now;
                    _app.Invoke(() => _viewModel.AddLog(new LogEntry
                    {
                        Timestamp = timestamp,
                        ConnectionName = "PacketGen",
                        Message = msg
                    }));

                    if (string.IsNullOrWhiteSpace(logFilePath)) return;
                    try
                    {
                        File.AppendAllText(logFilePath, $"[{timestamp:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
                    }
                    catch
                    {
                        // Ignore file write errors
                    }
                });
            });
            _app.RequestStop();
        };

        dialog.AddButton(runBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        _app.Run(dialog);
    }

    /// <summary>
    /// Opens a dialog to resolve a hostname or perform a reverse DNS lookup.
    /// </summary>
    private void OnDnsLookup()
    {
        var queryField = new TextField { Text = "google.com", X = 13, Y = 1, Width = 30 };
        var dialog = new Dialog { Title = "DNS Lookup", Width = 50, Height = 10 };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(new Label { Text = "Host/IP:", X = 1, Y = 1 }, queryField);

        var resolveBtn = new Button { Text = "Resolve", IsDefault = true };
        resolveBtn.Accepting += (s, e) =>
        {
            var query = queryField.Text;
            Task.Run(async () =>
            {
                try
                {
                    string result;
                    if (System.Net.IPAddress.TryParse(query, out _))
                    {
                        var host = await DnsHelper.ReverseLookupAsync(query);
                        result = host != null ? $"Hostname: {host}" : "No hostname found for this IP.";
                    }
                    else
                    {
                        var addresses = await DnsHelper.ResolveHostAsync(query);
                        result = addresses.Count != 0 
                            ? $"Addresses:\n{string.Join("\n", addresses.Select(a => a.ToString()))}" 
                            : "Could not resolve hostname.";
                    }

                    _app.Invoke(() => MessageBox.Query(_app, "DNS Results", result, "Ok"));
                }
                catch (Exception ex)
                {
                    _app.Invoke(() => MessageBox.ErrorQuery(_app, "DNS Error", ex.Message, "Ok"));
                }
            });
            _app.RequestStop();
        };

        dialog.AddButton(resolveBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);
        _app.Run(dialog);
    }

    /// <summary>
    /// Updates the details view with information about the currently selected connection.
    /// </summary>
    private void UpdateDetails()
    {
        if (_selectedInstance == null)
        {
            _detailsView.Text = "No connection selected.";
            return;
        }

        var config = _selectedInstance.Config;
        var autoTxInfo = config.AutoTransactions.Any()
            ? $"\nAuto-Tx: {config.AutoTransactions.Count} items, Interval: {config.IntervalMs?.ToString() ?? "On Receive"}, Next: {_selectedInstance.AutoTxIndex}"
            : "";
        var dumpInfo = !string.IsNullOrEmpty(config.DumpFilePath) ? $"\nDump File: {config.DumpFilePath}" : "";
        var proxyInfo = config.Type == ConnectionType.Proxy ? $"\nForwarding to: {config.RemoteHost}:{config.RemotePort}" : "";

        _detailsView.Text = $"""
                             Name: {config.Name}
                             Type: {config.Type}
                             Host: {config.Host}
                             Port: {config.Port}
                             Status: {_selectedInstance.Status}
                             Error: {_selectedInstance.LastError ?? "None"}{autoTxInfo}{dumpInfo}{proxyInfo}
                             """;
    }

    /// <summary>
    /// Opens a dialog to load a transaction file for the selected connection.
    /// </summary>
    private void OnLoadTransactions()
    {
        if (_selectedInstance == null)
        {
            MessageBox.ErrorQuery(_app, "Load Transactions", "No connection selected.", "Ok");
            return;
        }

        var autoTxLabel = new Label { Text = "Transactions (one per line):", X = 1, Y = 1 };
        var autoTxField = new Editor { 
            X = 1, Y = 2, 
            Width = Dim.Fill() - 2, Height = 8,
            Text = string.Join("\n", _selectedInstance.Config.AutoTransactions.Select(t => t.Data))
        };

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
        var intervalField = new TextField { 
            X = Pos.Right(intervalLabel) + 1, Y = 13, Width = 10,
            Text = _selectedInstance.Config.IntervalMs?.ToString() ?? ""
        };

        var jitterLabel = new Label { Text = "Jitter Min/Max (ms):", X = 1, Y = 14 };
        var jitterMinField = new TextField { 
            X = Pos.Right(jitterLabel) + 1, Y = 14, Width = 8,
            Text = _selectedInstance.Config.JitterMinMs?.ToString() ?? ""
        };
        var jitterMaxField = new TextField { 
            X = Pos.Right(jitterMinField) + 1, Y = 14, Width = 8,
            Text = _selectedInstance.Config.JitterMaxMs?.ToString() ?? ""
        };

        var dialog = new Dialog {
            Title = "Load Transactions",
            Width = 60, Height = 20
        };
        dialog.SetScheme(this.GetScheme());
        dialog.Add(autoTxLabel, autoTxField, loadFileBtn, intervalLabel, intervalField, jitterLabel, jitterMinField, jitterMaxField);

        var updateBtn = new Button { Text = "Update", IsDefault = true };
        updateBtn.Accepting += (_,_) => {
            var lines = autoTxField.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var transactions = lines.Select(line => new Transaction { Data = line, Encoding = TransactionEncoding.Ascii }).ToList();

            int? interval = int.TryParse(intervalField.Text, out var i) ? i : null;
            int? jitterMin = int.TryParse(jitterMinField.Text, out var jmin) ? jmin : null;
            int? jitterMax = int.TryParse(jitterMaxField.Text, out var jmax) ? jmax : null;

            _selectedInstance.UpdateAutoTransactions(transactions, interval, jitterMin, jitterMax);
            UpdateDetails();
            _app.RequestStop();
        };
        dialog.AddButton(updateBtn);

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(cancelBtn);

        _app.Run(dialog);
    }

    /// <summary>
    /// Clears the logs from the log view.
    /// </summary>
    private void OnClearLogs()
    {
        _viewModel.ClearLogs();
    }

    /// <summary>
    /// Displays an "about" dialog with project information.
    /// </summary>
    private void OnAbout()
    {
        var dialog = new Dialog { Title = "About TUI TCP Tester", Width = 60, Height = 10 };
        dialog.SetScheme(this.GetScheme());

        var blurb = new Label
        {
            Text = "A terminal-based TCP testing tool for developers.\nQuickly test server and client connections.",
            X = Pos.Center(),
            Y = 1,
            TextAlignment = Alignment.Center
        };

        if (GetScheme() != null)
        {
            var link = new Label
            {
                Text = "https://github.com/cmbsolver/tuitcptester",
                X = Pos.Center(),
                Y = 4
            };
                link.SetScheme(new Scheme
                { Normal = new Attribute(Color.BrightBlue, GetScheme().Normal.Background) });

            dialog.Add(blurb, link);
        }

        var okBtn = new Button { Text = "Ok", IsDefault = true };
        okBtn.Accepting += (_,_) => _app.RequestStop();
        dialog.AddButton(okBtn);

        _app.Run(dialog);
    }
}
