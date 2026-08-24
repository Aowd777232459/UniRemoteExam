namespace UniRemoteExam.Client;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage)
    {
        InitializeComponent();
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var navigationPage = new NavigationPage(_mainPage)
        {
            BarBackgroundColor = Color.FromArgb("#073B31"),
            BarTextColor = Colors.White
        };

        return new Window(navigationPage)
        {
            Title = "نظام الاختبارات الجامعي"
        };
    }
}
