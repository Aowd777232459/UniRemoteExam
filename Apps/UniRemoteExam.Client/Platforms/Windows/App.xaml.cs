using Microsoft.Maui;

namespace UniRemoteExam.Client.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        ConfigureWebView2UserDataFolder();
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static void ConfigureWebView2UserDataFolder()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userDataFolder = Path.Combine(localAppData, "UniRemoteExam", "WebView2");

        Directory.CreateDirectory(userDataFolder);
        Environment.SetEnvironmentVariable(
            "WEBVIEW2_USER_DATA_FOLDER",
            userDataFolder,
            EnvironmentVariableTarget.Process);
    }
}
