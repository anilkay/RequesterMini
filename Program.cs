using Avalonia;
using ReactiveUI.Avalonia;
using RequesterMini.Utils;
using System;

namespace RequesterMini;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                AppLogger.Error("Unhandled exception", ex);
            }
            else
            {
                AppLogger.Error("Unhandled non-exception error occurred.");
            }
        };

        AppLogger.Info("Application starting.");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        AppLogger.Info("Application stopped.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
