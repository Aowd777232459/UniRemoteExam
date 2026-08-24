namespace UniRemoteExam.Client.Services;

public sealed class ServerEndpointStore
{
    private const string PreferenceKey = "UniRemoteExam.ServerUrl";

    public Uri? Get()
    {
        var saved = Preferences.Default.Get(PreferenceKey, string.Empty);
        return Uri.TryCreate(saved, UriKind.Absolute, out var uri) ? Normalize(uri) : null;
    }

    public bool TrySave(string? value, out Uri? endpoint, out string error)
    {
        endpoint = null;
        error = string.Empty;
        var candidate = (value ?? string.Empty).Trim();

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            error = "أدخل رابطًا صحيحًا للخادم، مثل https://exam.example.com";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "لأمان حسابات وبيانات الطلاب يجب أن يبدأ رابط الخادم بـ https://";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "لا تضع اسم مستخدم أو كلمة مرور داخل رابط الخادم.";
            return false;
        }

        endpoint = Normalize(uri);
        Preferences.Default.Set(PreferenceKey, endpoint.AbsoluteUri);
        return true;
    }

    private static Uri Normalize(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Path = uri.AbsolutePath.TrimEnd('/') + "/",
            Query = string.Empty,
            Fragment = string.Empty
        };
        return builder.Uri;
    }
}
