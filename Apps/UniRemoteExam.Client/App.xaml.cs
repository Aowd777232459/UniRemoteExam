using UniRemoteExam.Client.Services;

namespace UniRemoteExam.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigationPage = new NavigationPage(new MainPage(new ServerEndpointStore()))
        {
            BarBackgroundColor = Color.FromArgb("#073B31"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage)
        {
            Title = "نظام الاختبارات الإلكترونية"
        };
    }
}
