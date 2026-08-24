using UniRemoteExam.Client.Services;

namespace UniRemoteExam.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<ServerEndpointStore>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
