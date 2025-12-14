using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ginger.Integration;
using Ginger.Services;
using Ginger.ViewModels;
using Ginger.Views;

namespace Ginger;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Initialize locales and dictionaries
        LocaleService.Init();
        DictionaryService.Load();

        // Load settings on startup
        AppSettings.Load();

        // Auto-connect to Backyard if enabled
        InitializeBackyardConnection();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            // Restore window position and size if valid
            if (AppSettings.User.WindowWidth > 0 && AppSettings.User.WindowHeight > 0)
            {
                mainWindow.Width = AppSettings.User.WindowWidth;
                mainWindow.Height = AppSettings.User.WindowHeight;
            }

            if (AppSettings.User.WindowX > 0 || AppSettings.User.WindowY > 0)
            {
                mainWindow.Position = new PixelPoint(
                    (int)AppSettings.User.WindowX,
                    (int)AppSettings.User.WindowY);
            }

            if (AppSettings.User.WindowMaximized)
            {
                mainWindow.WindowState = Avalonia.Controls.WindowState.Maximized;
            }

            // Save settings when window closes
            mainWindow.Closing += (_, _) =>
            {
                // Save window state
                if (mainWindow.WindowState == Avalonia.Controls.WindowState.Normal)
                {
                    AppSettings.User.WindowX = mainWindow.Position.X;
                    AppSettings.User.WindowY = mainWindow.Position.Y;
                    AppSettings.User.WindowWidth = mainWindow.Width;
                    AppSettings.User.WindowHeight = mainWindow.Height;
                }
                AppSettings.User.WindowMaximized = mainWindow.WindowState == Avalonia.Controls.WindowState.Maximized;

                AppSettings.Save();
            };

            // Also save on exit
            desktop.ShutdownRequested += (_, _) =>
            {
                AppSettings.Save();
            };

            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void InitializeBackyardConnection()
    {
        if (!AppSettings.BackyardLink.Enabled)
            return;

        try
        {
            // Check if we have a stored version and it matches the current Backyard version
            if (Backyard.GetAppVersion(out var appVersion)
                && AppSettings.BackyardLink.LastVersion.Major == appVersion.Major
                && AppSettings.BackyardLink.LastVersion.Minor == appVersion.Minor)
            {
                // Try to establish connection
                if (Backyard.EstablishConnection() == Backyard.Error.NoError)
                {
                    Backyard.RefreshCharacters();
                }
                else
                {
                    // Connection failed - disable
                    AppSettings.BackyardLink.Enabled = false;
                }
            }
            else
            {
                // Version mismatch or no previous version - require re-linking
                AppSettings.BackyardLink.Enabled = false;
                AppSettings.BackyardLink.Strict = true;
            }
        }
        catch
        {
            // Any error - disable connection
            AppSettings.BackyardLink.Enabled = false;
        }
    }
}
