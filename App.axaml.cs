using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RequesterMini.ViewModels;
using RequesterMini.Views;

namespace RequesterMini;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
                var collection = new ServiceCollection();

                collection.AddHttpClient();
                collection.AddTransient<MainWindowViewModel>();
                var services = collection.BuildServiceProvider();
                

                var vm = services.GetRequiredService<MainWindowViewModel>();



        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm,
            };
            
        }

        base.OnFrameworkInitializationCompleted();
    }
}