using Terminal.Gui;
using tuitcptester.Logic;
using System.Collections.ObjectModel;
using Attribute = Terminal.Gui.Attribute;

namespace tuitcptester.UI;

/// <summary>
/// The main user interface for the TCP Test Tool, using Terminal.Gui.
/// </summary>
public sealed partial class MainView : Toplevel
{
    private MenuBar _menu;
    private ListView _connectionList;
    private TextView _detailsView;
    private ListView _logView;
    private ObservableCollection<TcpInstance> _instances = new();
    private ObservableCollection<string> _logs = new();
    private TcpInstance? _selectedInstance;

    private static readonly ColorScheme GreenScreen = new ColorScheme
    {
        Normal = new Attribute(Color.Green, Color.Black),
        Focus = new Attribute(Color.Black, Color.Green),
        HotNormal = new Attribute(Color.BrightGreen, Color.Black),
        HotFocus = new Attribute(Color.BrightGreen, Color.Green)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MainView"/> class and sets up the UI components.
    /// </summary>
    public MainView()
    {
        Title = "TCP Test Tool";

        _menu = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File", new MenuItem[]
                {
                    new("_Save Configuration", "", OnSaveConfig),
                    new("_Load Configuration", "", OnLoadConfig),
                    new("_Quit", "Ctrl+Q", () => Application.RequestStop())
                }),
                new MenuBarItem("_New", new MenuItem[]
                {
                    new("_Server", "F2", OnNewServer),
                    new("_Client", "F3", OnNewClient),
                }),
                new MenuBarItem("_Control", new MenuItem[]
                {
                    new("_Start", "F4", OnStartConnection),
                    new("S_top", "F5", OnStopConnection),
                    new("_Remove", "F6", OnDisposeConnection),
                    new("Send _Message", "F7", OnSendManual),
                }),
                new MenuBarItem("_Tools", new MenuItem[]
                {
                    new("_Ping", "F8", OnPing)
                }),
                new MenuBarItem("T_hemes", CreateThemeMenuItems())
            ]
        };

        var topHalf = new FrameView
        {
            Title = "Connections",
            X = 0, Y = 1, Width = Dim.Percent(50), Height = Dim.Percent(50)
        };

        _connectionList = new ListView
        {
            Source = new ListWrapper<TcpInstance>(_instances),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        _connectionList.SelectedItemChanged += (s, e) =>
        {
            _selectedInstance = _instances.Count > e.Item ? _instances[e.Item] : null;
            UpdateDetails();
        };
        topHalf.Add(_connectionList);

        var detailsFrame = new FrameView
        {
            Title = "Details",
            X = Pos.Right(topHalf), Y = 1, Width = Dim.Fill(), Height = Dim.Percent(50)
        };
        _detailsView = new TextView
        {
            Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true
        };
        detailsFrame.Add(_detailsView);

        var bottomHalf = new FrameView
        {
            Title = "Logs",
            X = 0, Y = Pos.Bottom(topHalf), Width = Dim.Fill(), Height = Dim.Fill()
        };
        _logView = new ListView
        {
            Source = new ListWrapper<string>(_logs),
            Width = Dim.Fill(), Height = Dim.Fill()
        };
        bottomHalf.Add(_logView);

        Add(_menu, topHalf, detailsFrame, bottomHalf);

        // Set the default theme to Green Screen
        ColorScheme = GreenScreen;
        foreach (var view in Subviews)
        {
            view.ColorScheme = ColorScheme;
        }

        // Key bindings
        KeyDown += (s, e) =>
        {
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
        var themes = new Dictionary<string, ColorScheme>
        {
            { "Green Screen (Default)", GreenScreen },
            {
                "Blue", new ColorScheme
                {
                    Normal = new Attribute(Color.Gray, Color.Blue),
                    Focus = new Attribute(Color.White, Color.DarkGray),
                    HotNormal = new Attribute(Color.BrightCyan, Color.Blue),
                    HotFocus = new Attribute(Color.BrightCyan, Color.DarkGray)
                }
            },
            {
                "Cyberpunk", new ColorScheme
                {
                    Normal = new Attribute(Color.BrightMagenta, Color.Black),
                    Focus = new Attribute(Color.Black, Color.BrightCyan),
                    HotNormal = new Attribute(Color.Cyan, Color.Black),
                    HotFocus = new Attribute(Color.Cyan, Color.BrightCyan)
                }
            },
            {
                "Red Alert", new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Red),
                    Focus = new Attribute(Color.Black, Color.BrightRed),
                    HotNormal = new Attribute(Color.Yellow, Color.Red),
                    HotFocus = new Attribute(Color.Yellow, Color.BrightRed)
                }
            },
            {
                "Orange Screen", new ColorScheme
                {
                    Normal = new Attribute(Color.Black, Color.BrightYellow),
                    Focus = new Attribute(Color.White, Color.Black),
                    HotNormal = new Attribute(Color.Black, Color.BrightYellow),
                    HotFocus = new Attribute(Color.BrightYellow, Color.Black)
                }
            },
            {
                "Purple", new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Magenta),
                    Focus = new Attribute(Color.Black, Color.BrightMagenta),
                    HotNormal = new Attribute(Color.Yellow, Color.Magenta),
                    HotFocus = new Attribute(Color.Yellow, Color.BrightMagenta)
                }
            },
            {
                "Monochrome", new ColorScheme
                {
                    Normal = new Attribute(Color.Gray, Color.Black),
                    Focus = new Attribute(Color.Black, Color.White),
                    HotNormal = new Attribute(Color.White, Color.Black),
                    HotFocus = new Attribute(Color.White, Color.White)
                }
            },
            {
                "Midnight", new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Blue),
                    Focus = new Attribute(Color.Blue, Color.BrightCyan),
                    HotNormal = new Attribute(Color.BrightCyan, Color.Blue),
                    HotFocus = new Attribute(Color.White, Color.BrightCyan)
                }
            },
            {
                "Matrix", new ColorScheme
                {
                    Normal = new Attribute(Color.BrightGreen, Color.Black),
                    Focus = new Attribute(Color.Black, Color.Green),
                    HotNormal = new Attribute(Color.Green, Color.Black),
                    HotFocus = new Attribute(Color.BrightGreen, Color.Green)
                }
            },
            {
                "Solarized Dark", new ColorScheme
                {
                    Normal = new Attribute(Color.Gray, Color.Black),
                    Focus = new Attribute(Color.White, Color.DarkGray),
                    HotNormal = new Attribute(Color.BrightYellow, Color.Black),
                    HotFocus = new Attribute(Color.BrightYellow, Color.DarkGray)
                }
            }
        };

        return themes.Select(kvp => new MenuItem(kvp.Key, "", () => ApplyTheme(kvp.Value))).ToArray();
    }

    private void ApplyTheme(ColorScheme scheme)
    {
        ColorScheme = scheme;
        foreach (var view in Subviews)
        {
            view.ColorScheme = scheme;
            view.SetNeedsLayout();
        }

        SetNeedsLayout();
    }
}
