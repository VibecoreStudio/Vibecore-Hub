namespace VibecoreHub.Desktop;

public partial class App : System.Windows.Application
{
    private Platform.SingleInstance? _singleInstance;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new Platform.SingleInstance();
        if (!_singleInstance.IsFirstInstance)
        {
            Platform.SingleInstance.ActivateExistingWindow();
            Shutdown();
            return;
        }
        ThemeService.Apply(SettingsStore.Load().Theme);
        new MainWindow().Show();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
