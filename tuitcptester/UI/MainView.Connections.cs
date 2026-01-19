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
            Width = 40, Height = 10,
            ColorScheme = ColorScheme
        };
        var label = new Label { Text = "Port: ", X = 1, Y = 1 };
        var portField = new TextField { Text = "", X = Pos.Right(label), Y = 1, Width = 20 };
        dialog.Add(label, portField);
        
        var startBtn = new Button { Text = "Start", IsDefault = true };
        startBtn.Accepting += (s, e) =>
        {
            if (!int.TryParse(portField.Text, out var port)) return;
            var config = new TcpConfiguration { Name = $"Server:{port}", Type = ConnectionType.Server, Port = port };
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

        var dialog = new Dialog {
            Title = "New TCP Client",
            Width = 50, Height = 12,
            ColorScheme = ColorScheme
        };
        dialog.Add(hostLabel, hostField, portLabel, portField);

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
