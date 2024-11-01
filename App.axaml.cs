using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace CheckApp
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

       public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var secondWindow = new SecondWindow();
        var mainWindow = new MainWindow(secondWindow);

        mainWindow.Show();
        secondWindow.Show();
    }

    base.OnFrameworkInitializationCompleted();
}

    }
}
