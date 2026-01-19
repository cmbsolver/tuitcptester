using Terminal.Gui;
using tuitcptester.UI;

namespace tuitcptester;

/// <summary>
/// The entry point class for the application.
/// </summary>
public static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">The command-line arguments. The first argument can be a path to a configuration file.</param>
    public static void Main(string[] args)
    {
        Application.Init();

        try
        {
            var mainView = new MainView();
            if (args.Length > 0 && File.Exists(args[0]))
            {
                mainView.LoadConfig(args[0]);
            }
            Application.Run(mainView);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}