using Terminal.Gui;
using tuitcptester.Logic;
using tuitcptester.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using Attribute = Terminal.Gui.Attribute;

namespace tuitcptester.UI;

/// <summary>
/// The main user interface for the TCP Test Tool, using Terminal.Gui.
/// </summary>
public sealed class MainView : Toplevel
{
    private MenuBar _menu;
    private ListView _connectionList;
    private TextView _detailsView;
    private ListView _logView;
    private ObservableCollection<TcpInstance> _instances = new();
    private ObservableCollection<string> _logs = new();
    private TcpInstance? _selectedInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainView"/> class and sets up the UI components.
    /// </summary>
    public MainView()
    {
        Title = "TCP Test Tool";
        
        _menu = new MenuBar {
            Menus =
            [
                new MenuBarItem("_File", new MenuItem[] {
                    new("_Save Configuration", "", OnSaveConfig),
                    new("_Load Configuration", "", OnLoadConfig),
                    new("_Quit", "Ctrl+Q", () => Application.RequestStop())
                }),
                new MenuBarItem("_New", new MenuItem[] {
                    new("_Server", "F2", OnNewServer),
                    new("_Client", "F3", OnNewClient),
                }),
                new MenuBarItem("_Control", new MenuItem[] {
                    new("_Start", "F4", OnStartConnection),
                    new("S_top", "F5", OnStopConnection),
                    new("_Remove", "F6", OnDisposeConnection),
                    new("Send _Message", "F7", OnSendManual),
                }),
                new MenuBarItem("_Tools", new MenuItem[] {
                    new("_Ping", "F8", OnPing)
                }),
                new MenuBarItem("T_hemes", CreateThemeMenuItems())
            ]
        };

        var topHalf = new FrameView {
            Title = "Connections",
            X = 0, Y = 1, Width = Dim.Percent(50), Height = Dim.Percent(50)
        };
        
        _connectionList = new ListView {
            Source = new ListWrapper<TcpInstance>(_instances),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        _connectionList.SelectedItemChanged += (s, e) => {
            _selectedInstance = _instances.Count > e.Item ? _instances[e.Item] : null;
            UpdateDetails();
        };
        topHalf.Add(_connectionList);

        var detailsFrame = new FrameView {
            Title = "Details",
            X = Pos.Right(topHalf), Y = 1, Width = Dim.Fill(), Height = Dim.Percent(50)
        };
        _detailsView = new TextView {
            Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true
        };
        detailsFrame.Add(_detailsView);

        var bottomHalf = new FrameView {
            Title = "Logs",
            X = 0, Y = Pos.Bottom(topHalf), Width = Dim.Fill(), Height = Dim.Fill()
        };
        _logView = new ListView {
            Source = new ListWrapper<string>(_logs),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        bottomHalf.Add(_logView);

        Add(_menu, topHalf, detailsFrame, bottomHalf);

        // Ensure subviews use the initial color scheme
        ColorScheme = Colors.ColorSchemes["Base"];
        foreach (var view in Subviews)
        {
            view.ColorScheme = ColorScheme;
        }

        // Key bindings
        KeyDown += (s, e) => {
            if (e.KeyCode == Key.F2) OnNewServer();
            if (e.KeyCode == Key.F3) OnNewClient();
            if (e.KeyCode == Key.F4) OnStopConnection();
            if (e.KeyCode == Key.F5) OnStartConnection();
            if (e.KeyCode == Key.F6) OnDisposeConnection();
            if (e.KeyCode == Key.F7) OnSendManual();
            if (e.KeyCode == Key.F8) OnPing();
        };
    }

    /// <summary>
    /// Creates the menu items for switching UI themes.
    /// </summary>
    /// <returns>An array of <see cref="MenuItem"/> objects.</returns>
    private MenuItem[] CreateThemeMenuItems()
    {
        var themes = new Dictionary<string, ColorScheme> {
            { "Blue (Default)", new ColorScheme {
                Normal = new Attribute(Color.Gray, Color.Blue),
                Focus = new Attribute(Color.White, Color.DarkGray),
                HotNormal = new Attribute(Color.BrightCyan, Color.Blue),
                HotFocus = new Attribute(Color.BrightCyan, Color.DarkGray)
            }},
            { "Green Screen", new ColorScheme {
                Normal = new Attribute(Color.Green, Color.Black),
                Focus = new Attribute(Color.Black, Color.Green),
                HotNormal = new Attribute(Color.BrightGreen, Color.Black),
                HotFocus = new Attribute(Color.BrightGreen, Color.Green)
            }},
            { "Orange Screen", new ColorScheme {
                Normal = new Attribute(Color.Black, Color.BrightYellow),
                Focus = new Attribute(Color.White, Color.Black),
                HotNormal = new Attribute(Color.Black, Color.BrightYellow),
                HotFocus = new Attribute(Color.BrightYellow, Color.Black)
            }},
            { "Purple", new ColorScheme {
                Normal = new Attribute(Color.White, Color.Magenta),
                Focus = new Attribute(Color.Black, Color.BrightMagenta),
                HotNormal = new Attribute(Color.Yellow, Color.Magenta),
                HotFocus = new Attribute(Color.Yellow, Color.BrightMagenta)
            }}
        };

        return themes.Select(kvp => new MenuItem(kvp.Key, "", () => {
            ColorScheme = kvp.Value;
            foreach (var view in Subviews)
            {
                view.ColorScheme = kvp.Value;
                view.SetNeedsLayout();
            }
            SetNeedsLayout();
        })).ToArray();
    }

    // Inside your theme switcher logic

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
        _detailsView.Text = $"""
            Name: {config.Name}
            Type: {config.Type}
            Host: {config.Host}
            Port: {config.Port}
            Status: {_selectedInstance.Status}
            Error: {_selectedInstance.LastError ?? "None"}
            """;
    }

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
        var dataField = new TextField { Text = "", X = 1, Y = 2, Width = Dim.Fill() - 2 };
        
        var encodingLabel = new Label { Text = "Encoding:", X = 1, Y = 4 };
        var encodingGroup = new RadioGroup 
        { 
            X = 1, Y = 5,
            RadioLabels = ["ASCII", "Hex", "Binary (Base64)"]
        };
        
        var dialog = new Dialog { Title = "Manual Send", Width = 60, Height = 13, ColorScheme = ColorScheme };
        dialog.Add(dataLabel, dataField, encodingLabel, encodingGroup);

        var sendBtn = new Button { Text = "Send", IsDefault = true };
        sendBtn.Accepting += (s, e) => {
            var tx = new Transaction {
                Data = dataField.Text,
                Encoding = (TransactionEncoding)encodingGroup.SelectedItem
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
        if (_selectedInstance == null) return;
        _selectedInstance.Stop();
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
