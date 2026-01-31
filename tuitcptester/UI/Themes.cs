using Terminal.Gui.Drawing;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace tuitcptester.UI;

/// <summary>
/// Provides a collection of UI themes for the application.
/// </summary>
public static class Themes
{
    /// <summary>
    /// Gets a custom color scheme inspired by classic "green screen" terminals.
    /// </summary>
    public static Scheme GreenScreen { get; } = new Scheme
    {
        Normal = new Attribute(Color.Green, Color.Black),
        Focus = new Attribute(Color.Black, Color.Green),
        HotNormal = new Attribute(Color.BrightGreen, Color.Black),
        HotFocus = new Attribute(Color.BrightGreen, Color.Green)
    };

    /// <summary>
    /// Gets a dictionary of all available color schemes, indexed by their display names.
    /// </summary>
    public static Dictionary<string, Scheme> All { get; } = new Dictionary<string, Scheme>
    {
        { "Green Screen (Default)", GreenScreen },
        {
            "Blue", new Scheme
            {
                Normal = new Attribute(Color.Gray, Color.Blue),
                Focus = new Attribute(Color.White, Color.DarkGray),
                HotNormal = new Attribute(Color.BrightCyan, Color.Blue),
                HotFocus = new Attribute(Color.BrightCyan, Color.DarkGray)
            }
        },
        {
            "Cyberpunk", new Scheme
            {
                Normal = new Attribute(Color.BrightMagenta, Color.Black),
                Focus = new Attribute(Color.Black, Color.BrightCyan),
                HotNormal = new Attribute(Color.Cyan, Color.Black),
                HotFocus = new Attribute(Color.Cyan, Color.BrightCyan)
            }
        },
        {
            "Cypherpunk", new Scheme
            {
                Normal = new Attribute(Color.Gray, Color.Black),
                Focus = new Attribute(Color.Black, Color.BrightCyan),
                HotNormal = new Attribute(Color.BrightYellow, Color.Black),
                HotFocus = new Attribute(Color.BrightYellow, Color.BrightCyan)
            }
        },
        {
            "Cypherpunk (Neon Green)", new Scheme
            {
                Normal = new Attribute(Color.Gray, Color.Black),
                Focus = new Attribute(Color.Black, Color.BrightGreen),
                HotNormal = new Attribute(Color.BrightGreen, Color.Black),
                HotFocus = new Attribute(Color.Black, Color.BrightGreen)
            }
        },
        {
            "Cypherpunk (Cool Blue CRT)", new Scheme
            {
                Normal = new Attribute(Color.White, Color.Black),
                Focus = new Attribute(Color.Black, Color.Cyan),
                HotNormal = new Attribute(Color.Cyan, Color.Black),
                HotFocus = new Attribute(Color.Black, Color.Cyan)
            }
        },
        {
            "Red Alert", new Scheme
            {
                Normal = new Attribute(Color.White, Color.Red),
                Focus = new Attribute(Color.Black, Color.BrightRed),
                HotNormal = new Attribute(Color.Yellow, Color.Red),
                HotFocus = new Attribute(Color.Yellow, Color.BrightRed)
            }
        },
        {
            "Old Yeller", new Scheme
            {
                Normal = new Attribute(Color.Black, Color.BrightYellow),
                Focus = new Attribute(Color.White, Color.Black),
                HotNormal = new Attribute(Color.Black, Color.BrightYellow),
                HotFocus = new Attribute(Color.BrightYellow, Color.Black)
            }
        },
        {
            "Purple", new Scheme
            {
                Normal = new Attribute(Color.White, Color.Magenta),
                Focus = new Attribute(Color.Black, Color.BrightMagenta),
                HotNormal = new Attribute(Color.Yellow, Color.Magenta),
                HotFocus = new Attribute(Color.Yellow, Color.BrightMagenta)
            }
        },
        {
            "Midnight", new Scheme
            {
                Normal = new Attribute(Color.White, Color.Blue),
                Focus = new Attribute(Color.Blue, Color.BrightCyan),
                HotNormal = new Attribute(Color.BrightCyan, Color.Blue),
                HotFocus = new Attribute(Color.White, Color.BrightCyan)
            }
        },
        {
            "Matrix", new Scheme
            {
                Normal = new Attribute(Color.BrightGreen, Color.Black),
                Focus = new Attribute(Color.Black, Color.Green),
                HotNormal = new Attribute(Color.Green, Color.Black),
                HotFocus = new Attribute(Color.BrightGreen, Color.Green)
            }
        },
        {
            "Solarized Dark", new Scheme
            {
                Normal = new Attribute(Color.Gray, Color.Black),
                Focus = new Attribute(Color.White, Color.DarkGray),
                HotNormal = new Attribute(Color.BrightYellow, Color.Black),
                HotFocus = new Attribute(Color.BrightYellow, Color.DarkGray)
            }
        }
    };
}
