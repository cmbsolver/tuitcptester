using System.Collections.ObjectModel;
using Terminal.Gui;
using tuitcptester.Logic;
using tuitcptester.Models;

namespace tuitcptester.UI;

public sealed partial class MainView
{
    /// <summary>
    /// Opens a dialog to manually send a message over the selected connection.
    /// </summary>
    private void OnSendManual()
    {
        if (!_instances.Any())
        {
            MessageBox.ErrorQuery("Send Message", "No connections exist. Please create a server or client first.",
                "Ok");
            return;
        }

        if (_selectedInstance == null)
        {
            MessageBox.ErrorQuery("Send Message", "Please select a connection from the list.", "Ok");
            return;
        }

        if (_selectedInstance.Status != ConnectionStatus.Connected &&
            _selectedInstance.Config.Type == ConnectionType.Client)
        {
            MessageBox.ErrorQuery("Send Message", "Client is not connected.", "Ok");
            return;
        }

        var dataLabel = new Label { Text = "Data (Hex/Base64/ASCII):", X = 1, Y = 1 };
        var dataField = new TextField { Text = "", X = 1, Y = 2, Width = Dim.Fill()! - 2 };

        var encodingLabel = new Label { Text = "Encoding:", X = 1, Y = 4 };
        var encodingGroup = new RadioGroup
        {
            X = 1, Y = 5,
            RadioLabels = ["ASCII", "Hex", "Binary (Base64)"]
        };

        var returnCheckbox = new CheckBox { Text = "Append \\r (Return)", X = 1, Y = 8 };
        var newlineCheckbox = new CheckBox { Text = "Append \\n (Newline)", X = 1, Y = 9 };

        var hintLabel = new Label { Text = "Press ESC to Cancel", X = 1, Y = 11, ColorScheme = ColorScheme };

        var dialog = new Dialog { Title = "Manual Send", Width = 60, Height = 16, ColorScheme = ColorScheme };
        dialog.Add(dataLabel, dataField, encodingLabel, encodingGroup, returnCheckbox, newlineCheckbox, hintLabel);

        var sendBtn = new Button { Text = "Send", IsDefault = true };

        bool sendClicked = false;

        sendBtn.Accepting += (s, e) =>
        {
            sendClicked = true;
            Application.RequestStop();
        };

        dialog.AddButton(sendBtn);

        // Run the dialog modally
        Application.Run(dialog);

        // If the user closed the dialog via Escape or the Cancel button, 
        // sendClicked will be false.
        if (sendClicked)
        {
            var tx = new Transaction
            {
                Data = dataField.Text,
                Encoding = (TransactionEncoding)encodingGroup.SelectedItem,
                AppendReturn = returnCheckbox.CheckedState == CheckState.Checked,
                AppendNewline = newlineCheckbox.CheckedState == CheckState.Checked
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

        var dialog = new Dialog { Title = "Machine IP Addresses", Width = 60, Height = 12, ColorScheme = ColorScheme };
        var list = new ListView
        {
            Source = new ListWrapper<string>(ipAddresses),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 1
        };
        dialog.Add(list);

        var okBtn = new Button { Text = "Ok", IsDefault = true };
        okBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(okBtn);

        Application.Run(dialog);
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
            MessageBox.ErrorQuery("Start Error", ex.Message, "Ok");
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
        var dialog = new Dialog { Title = "Ping IP", Width = 50, Height = 10, ColorScheme = ColorScheme };
        dialog.Add(new Label { Text = "IP Address:", X = 1, Y = 1 }, hostField);

        var pingBtn = new Button { Text = "Ping", IsDefault = true };
        pingBtn.Accepting += (s, e) =>
        {
            string host = hostField.Text;
            Task.Run(() =>
            {
                try
                {
                    var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(host);
                    Application.Invoke(() => MessageBox.Query("Ping Result",
                        $"Status: {reply.Status}\nTime: {reply.RoundtripTime}ms", "Ok"));
                }
                catch (Exception ex)
                {
                    Application.Invoke(() => MessageBox.ErrorQuery("Ping Error", ex.Message, "Ok"));
                }
            });
            Application.RequestStop();
        };
        dialog.AddButton(pingBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(cancelBtn);
        Application.Run(dialog);
    }

    /// <summary>
    /// Opens a dialog to scan a range of ports on a host.
    /// </summary>
    private void OnPortScan()
    {
        var hostField = new TextField { Text = "127.0.0.1", X = 13, Y = 1, Width = 30 };
        var startPortField = new TextField { Text = "1", X = 13, Y = 2, Width = 10 };
        var endPortField = new TextField { Text = "65535", X = 13, Y = 3, Width = 10 };

        var dialog = new Dialog { Title = "Port Scan", Width = 50, Height = 12, ColorScheme = ColorScheme };
        dialog.Add(
            new Label { Text = "Host:", X = 1, Y = 1 }, hostField,
            new Label { Text = "Start Port:", X = 1, Y = 2 }, startPortField,
            new Label { Text = "End Port:", X = 1, Y = 3 }, endPortField
        );

        var scanBtn = new Button { Text = "Scan", IsDefault = true };
        scanBtn.Accepting += (s, e) =>
        {
            string host = hostField.Text;
            if (!int.TryParse(startPortField.Text, out int startPort) ||
                !int.TryParse(endPortField.Text, out int endPort))
            {
                MessageBox.ErrorQuery("Input Error", "Invalid port range.", "Ok");
                return;
            }

            if (startPort > endPort || startPort < 1 || endPort > 65535)
            {
                MessageBox.ErrorQuery("Input Error", "Ports must be between 1 and 65535, and Start <= End.", "Ok");
                return;
            }


            Task.Run(async () =>
            {
                try
                {
                    var results = await PortScanner.ScanRangeAsync(host, startPort, endPort);
                    var openPorts = results.Where(r => r.IsOpen).Select(r => r.Port).ToList();

                    Application.Invoke(() =>
                    {
                        var timestamp = DateTime.Now;
                        foreach (var port in openPorts)
                        {
                            var description = PortScanner.GetPortDescription(port);
                            _logs.Insert(0, $"[{timestamp:HH:mm:ss}] [Scanner] Open port {port} ({description}) found on {host}");
                            if (_logs.Count > 50) _logs.RemoveAt(50);
                        }

                        if (openPorts.Count != 0)
                        {
                            MessageBox.Query("Scan Results", $"Open ports on {host}:\n{string.Join(", ", openPorts)}\n\nResults also logged to the log area.", "Ok");
                        }
                        else
                        {
                            _logs.Insert(0, $"[{timestamp:HH:mm:ss}] [Scanner] No open ports found on {host} in range {startPort}-{endPort}");
                            if (_logs.Count > 50) _logs.RemoveAt(50);
                            MessageBox.Query("Scan Results", $"No open ports found on {host} in range {startPort}-{endPort}.", "Ok");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.Invoke(() => MessageBox.ErrorQuery("Scan Error", ex.Message, "Ok"));
                }
            });
            Application.RequestStop();
        };

        dialog.AddButton(scanBtn);
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(cancelBtn);
        Application.Run(dialog);
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

        _detailsView.Text = $"""
                             Name: {config.Name}
                             Type: {config.Type}
                             Host: {config.Host}
                             Port: {config.Port}
                             Status: {_selectedInstance.Status}
                             Error: {_selectedInstance.LastError ?? "None"}{autoTxInfo}{dumpInfo}
                             """;
    }

    /// <summary>
    /// Clears the logs from the log view.
    /// </summary>
    private void OnClearLogs()
    {
        _logs.Clear();
    }

    /// <summary>
    /// Displays an "about" dialog with project information.
    /// </summary>
    private void OnAbout()
    {
        var dialog = new Dialog { Title = "About TUI TCP Tester", Width = 60, Height = 10, ColorScheme = ColorScheme };

        var blurb = new Label
        {
            Text = "A terminal-based TCP testing tool for developers.\nQuickly test server and client connections.",
            X = Pos.Center(),
            Y = 1,
            TextAlignment = Alignment.Center
        };

        if (ColorScheme != null)
        {
            var link = new Label
            {
                Text = "https://github.com/cmbsolver/tuitcptester",
                X = Pos.Center(),
                Y = 4,
                ColorScheme = new ColorScheme
                    { Normal = new Terminal.Gui.Attribute(Color.BrightBlue, ColorScheme.Normal.Background) }
            };

            dialog.Add(blurb, link);
        }

        var okBtn = new Button { Text = "Ok", IsDefault = true };
        okBtn.Accepting += (s, e) => Application.RequestStop();
        dialog.AddButton(okBtn);

        Application.Run(dialog);
    }
}
