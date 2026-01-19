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
            MessageBox.ErrorQuery("Send Message", "No connections exist. Please create a server or client first.", "Ok");
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
        
        var dialog = new Dialog { Title = "Manual Send", Width = 60, Height = 16, ColorScheme = ColorScheme };
        dialog.Add(dataLabel, dataField, encodingLabel, encodingGroup, returnCheckbox, newlineCheckbox);

        var sendBtn = new Button { Text = "Send", IsDefault = true };
        sendBtn.Accepting += (s, e) => {
            var tx = new Transaction {
                Data = dataField.Text,
                Encoding = (TransactionEncoding)encodingGroup.SelectedItem,
                AppendReturn = returnCheckbox.CheckedState == CheckState.Checked,
                AppendNewline = newlineCheckbox.CheckedState == CheckState.Checked
            };
            
            _selectedInstance.SendManual(tx);
            Application.RequestStop();
        };
        
        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => Application.RequestStop();
        
        dialog.AddButton(sendBtn);
        dialog.AddButton(cancelBtn);
        Application.Run(dialog);
    }

    /// <summary>
    /// Starts the selected connection.
    /// </summary>
    private void OnStartConnection()
    {
        if (_selectedInstance == null) return;
        if (_selectedInstance.Status != ConnectionStatus.Disconnected && _selectedInstance.Status != ConnectionStatus.Error) return;

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
        var hostField = new TextField { Text = "127.0.0.1", Width = 30 };
        var dialog = new Dialog { Title = "Ping IP", Width = 50, Height = 10, ColorScheme = ColorScheme };
        dialog.Add(new Label { Text = "IP Address:", X = 1, Y = 1 }, hostField);
        
        var pingBtn = new Button { Text = "Ping", IsDefault = true };
        pingBtn.Accepting += (s, e) => {
            string host = hostField.Text;
            Task.Run(() => {
                try {
                    var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(host);
                    Application.Invoke(() => MessageBox.Query("Ping Result", $"Status: {reply.Status}\nTime: {reply.RoundtripTime}ms", "Ok"));
                } catch (Exception ex) {
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
            
        _detailsView.Text = $"""
                             Name: {config.Name}
                             Type: {config.Type}
                             Host: {config.Host}
                             Port: {config.Port}
                             Status: {_selectedInstance.Status}
                             Error: {_selectedInstance.LastError ?? "None"}{autoTxInfo}
                             """;
    }

    /// <summary>
    /// Clears the logs from the log view.
    /// </summary>
    private void OnClearLogs()
    {
        _logs.Clear();
    }
}
