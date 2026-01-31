using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Editor;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using tuitcptester.Logic;
using Terminal.Gui.Views;
using tuitcptester.ViewModels;

namespace tuitcptester.UI;

/// <summary>
/// The main user interface for the TCP Test Tool, using Terminal.Gui.
/// </summary>
public sealed partial class MainView : Window
{
    /// <summary>
    /// The application's interface
    /// </summary>
    private readonly IApplication _app;
    
    /// <summary>
    /// The view model that holds the application's state and logic.
    /// </summary>
    private readonly MainViewModel _viewModel;


    /// <summary>
    /// The text view displaying details for the selected connection.
    /// </summary>
    private readonly Editor _detailsView;


    /// <summary>
    /// The currently selected TCP connection instance, if any.
    /// </summary>
    private TcpInstance? _selectedInstance;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MainView"/> class and sets up the UI components.
    /// </summary>
    public MainView(IApplication app)
    {
        _app = app;
        _viewModel = new MainViewModel();
        Title = "TCP Test Tool";

        var menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File", new MenuItem[]
                {
                    new("_Save Configuration", "", OnSaveConfig),
                    new("_Load Configuration", "", OnLoadConfig),
                    new("_Export Logs", "", OnExportLogs),
                    new("_Quit", "Ctrl+Q", app.RequestStop)
                }),
                new MenuBarItem("_New", new MenuItem[]
                {
                    new("_Server", "F2", OnNewServer),
                    new("_Client", "F3", OnNewClient),
                    new("_Proxy", "F10", OnNewProxy),
                }),
                new MenuBarItem("_Control", new MenuItem[]
                {
                    new("_Start", "F4", OnStartConnection),
                    new("S_top", "F5", OnStopConnection),
                    new("_Remove", "F6", OnDisposeConnection),
                    new("_Load Transactions", "Ctrl+L", OnLoadTransactions),
                    new("Send _Message", "F7", OnSendManual),
                    new("_Clear Logs", "F9", OnClearLogs)
                }),
                new MenuBarItem("_Tools", new MenuItem[]
                {
                    new("_Ping", "F8", OnPing),
                    new("_Port Scan", "Ctrl+P", OnPortScan),
                    new("Port Transaction Scan", "Ctrl+T", OnPortTransactionScan),
                    new("_DNS Lookup", "Ctrl+D", OnDnsLookup),
                    new("Packet _Generator", "Ctrl+G", OnPacketGenerator),
                    new("_List IP Addresses", "", OnListIPs),
                }),
                new MenuBarItem("T_hemes", CreateThemeMenuItems()),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new("_About", "", OnAbout)
                })
            ]
        };

        var topHalf = new FrameView
        {
            Title = "Connections",
            X = 0, Y = 1, Width = Dim.Percent(50), Height = Dim.Percent(50)
        };

        var connectionList = new ListView
        {
            Source = new ListWrapper<TcpInstance>(_viewModel.Instances),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        connectionList.ValueChanged += (_, e) =>
        {
            _selectedInstance = _viewModel.Instances[e.NewValue ?? 0];
            UpdateDetails();
        };
        topHalf.Add(connectionList);

        var detailsFrame = new FrameView
        {
            Title = "Details",
            X = Pos.Right(topHalf), Y = 1, Width = Dim.Fill(), Height = Dim.Percent(50)
        };
        _detailsView = new Editor
        {
            Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true
        };
        detailsFrame.Add(_detailsView);

        var bottomHalf = new FrameView
        {
            Title = "Logs",
            X = 0, Y = Pos.Bottom(topHalf), Width = Dim.Fill(), Height = Dim.Fill()
        };
        var logView = new ListView
        {
            Source = new ListWrapper<LogEntry>(_viewModel.Logs),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        logView.ValueChanged += (_, e) =>
        {
            if (_viewModel.Logs.Count == 0)
            {
                return;
            }

            var index = Math.Clamp(e.NewValue ?? 0, 0, _viewModel.Logs.Count - 1);
            var log = _viewModel.Logs[index];
            MessageBox.Query(_app, "Log Entry", MainViewModel.FormatLogEntry(log), "Ok");
        };
        bottomHalf.Add(logView);

        Add(menu, topHalf, detailsFrame, bottomHalf);

        // Set the default theme to Green Screen
        this.SetScheme(Themes.GreenScreen);
        foreach (var view in SubViews)
        {
            view.SetScheme(Themes.GreenScreen);
        }

        // Key bindings
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Key.F2) OnNewServer();
            if (e.KeyCode == Key.F3) OnNewClient();
            if (e.KeyCode == Key.F4) OnStopConnection();
            if (e.KeyCode == Key.F5) OnStartConnection();
            if (e.KeyCode == Key.F6) OnDisposeConnection();
            if (e.KeyCode == Key.L.WithCtrl) OnLoadTransactions();
            if (e.KeyCode == Key.F7) OnSendManual();
            if (e.KeyCode == Key.F8) OnPing();
            if (e.KeyCode == Key.D.WithCtrl) OnDnsLookup();
            if (e.KeyCode == Key.G.WithCtrl) OnPacketGenerator();
            if (e.KeyCode == Key.T.WithCtrl) OnPortTransactionScan();
            if (e.KeyCode == Key.F9) OnClearLogs();
            if (e.KeyCode == Key.F10) OnNewProxy();
        };
    }

    /// <summary>
    /// Creates the menu items for switching UI themes.
    /// </summary>
    /// <returns>An array of <see cref="MenuItem"/> objects.</returns>
    private MenuItem[] CreateThemeMenuItems()
    {
        return Themes.All.Select(kvp => new MenuItem(kvp.Key, "", () => ApplyTheme(kvp.Value))).ToArray();
    }

    /// <summary>
    /// Applies the specified <see cref="Scheme"/> to the current view and all its subviews, ensuring that the layout reflects the changes.
    /// </summary>
    /// <param name="scheme">The color scheme to apply to the UI components.</param>
    private void ApplyTheme(Scheme scheme)
    {
        SetScheme(scheme);
        foreach (var view in SubViews)
        {
            view.SetScheme(scheme);
            view.SetNeedsLayout();
        }

        SetNeedsLayout();
    }
}
