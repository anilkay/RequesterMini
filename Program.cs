using AppLogger;
using Avalonia;
using ReactiveUI.Avalonia;
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
#if DEBUG
        Logger.Initialize("RequesterMini", LogLevel.Debug);
#else
        Logger.Initialize("RequesterMini");
#endif

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                Logger.Error("Unhandled exception", ex);
            }
            else
            {
                Logger.Error("Unhandled non-exception error occurred.");
            }
        };

        Logger.Info($"Application starting. MinLogLevel={Logger.MinimumLevel}");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        Logger.Info("Application stopped.");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
