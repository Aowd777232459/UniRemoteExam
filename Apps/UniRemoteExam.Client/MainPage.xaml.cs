using System.Net;
using UniRemoteExam.Client.Services;

namespace UniRemoteExam.Client;

public partial class MainPage : ContentPage
{
    private readonly ServerEndpointStore _endpointStore;
    private readonly HttpClient _healthClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private Uri? _endpoint;
    private bool _initialized;

    public MainPage(ServerEndpointStore endpointStore)
    {
        InitializeComponent();
        _endpointStore = endpointStore;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_initialized) return;

        _initialized = true;
        _endpoint = _endpointStore.Get();
        if (_endpoint is null)
        {
            ShowSettings(required: true);
            return;
        }

        ServerUrlEntry.Text = _endpoint.AbsoluteUri;
        await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        if (_endpoint is null)
        {
            ShowSettings(required: true);
            return;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            ShowConnectionError("لا يوجد اتصال بالإنترنت حاليًا. سيعاود التطبيق الاتصال عند عودة الشبكة.");
            return;
        }

        SetBusy(true, "فحص الخادم");
        try
        {
            using var response = await _healthClient.GetAsync(new Uri(_endpoint, "health"));
            if (!response.IsSuccessStatusCode)
            {
                var message = response.StatusCode == HttpStatusCode.ServiceUnavailable
                    ? "الخادم يعمل لكن قاعدة البيانات غير متصلة. راجع إعداد اتصال SQL Server."
                    : $"الخادم أعاد الحالة {(int)response.StatusCode}.";
                ShowConnectionError(message);
                return;
            }

            ConnectionMessage.IsVisible = false;
            SetStatus("متصل ومتزامن", "#16A34A");
            ServerWebView.Source = _endpoint.AbsoluteUri;
        }
        catch (TaskCanceledException)
        {
            ShowConnectionError("انتهت مهلة الاتصال بالخادم. تحقق من سرعة الشبكة وعنوان الخادم.");
        }
        catch (HttpRequestException)
        {
            ShowConnectionError("تعذر الوصول إلى الخادم. تأكد من صحة الرابط وشهادة HTTPS.");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
            await MainThread.InvokeOnMainThreadAsync(ConnectAsync);
        else
            MainThread.BeginInvokeOnMainThread(() => ShowConnectionError("انقطع اتصال الشبكة. لم تُفقد أي بيانات محفوظة على الخادم."));
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (_endpoint is null || !Uri.TryCreate(e.Url, UriKind.Absolute, out var target)) return;

        if (!string.Equals(target.Host, _endpoint.Host, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            _ = Launcher.Default.OpenAsync(target);
            return;
        }

        SetBusy(true, "تحميل");
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        SetBusy(false);
        if (e.Result == WebNavigationResult.Success)
        {
            ConnectionMessage.IsVisible = false;
            SetStatus("متصل ومتزامن", "#16A34A");
        }
        else
        {
            ShowConnectionError("تعذر تحميل الصفحة. تحقق من اتصال الشبكة ثم أعد المحاولة.");
        }
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        if (ServerWebView.CanGoBack) ServerWebView.GoBack();
    }

    private void OnHomeClicked(object? sender, EventArgs e)
    {
        if (_endpoint is not null) ServerWebView.Source = _endpoint.AbsoluteUri;
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await ConnectAsync();
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        ShowSettings(required: false);
    }

    private async void OnSaveEndpointClicked(object? sender, EventArgs e)
    {
        SettingsError.IsVisible = false;
        if (!_endpointStore.TrySave(ServerUrlEntry.Text, out var endpoint, out var error))
        {
            SettingsError.Text = error;
            SettingsError.IsVisible = true;
            return;
        }

        _endpoint = endpoint;
        SettingsOverlay.IsVisible = false;
        await ConnectAsync();
    }

    private void OnCancelSettingsClicked(object? sender, EventArgs e)
    {
        if (_endpoint is not null) SettingsOverlay.IsVisible = false;
    }

    private void ShowSettings(bool required)
    {
        ServerUrlEntry.Text = _endpoint?.AbsoluteUri ?? string.Empty;
        SettingsError.IsVisible = false;
        CancelSettingsButton.IsVisible = !required;
        SettingsOverlay.IsVisible = true;
    }

    private void ShowConnectionError(string message)
    {
        ConnectionMessageText.Text = message;
        ConnectionMessage.IsVisible = true;
        SetStatus("غير متصل", "#DC2626");
        SetBusy(false);
    }

    private void SetStatus(string text, string color)
    {
        StatusLabel.Text = text;
        StatusDot.Color = Color.FromArgb(color);
    }

    private void SetBusy(bool busy, string status = "")
    {
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        if (busy && !string.IsNullOrWhiteSpace(status)) SetStatus(status, "#F59E0B");
    }
}
