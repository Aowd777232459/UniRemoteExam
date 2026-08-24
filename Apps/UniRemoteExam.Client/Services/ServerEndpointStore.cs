using System.Net;

namespace UniRemoteExam.Client.Services;

public sealed class ServerEndpointStore
{
    private const string PreferenceKey = "UniRemoteExam.ServerUrl";

    public Uri? Get()
    {
        var saved = Preferences.Default.Get(PreferenceKey, string.Empty);
        if (Uri.TryCreate(saved, UriKind.Absolute, out var uri) && IsAllowed(uri))
            return Normalize(uri);

#if WINDOWS
        return new Uri("http://127.0.0.1:5113/");
#else
        return null;
#endif
    }

    public bool TrySave(string? value, out Uri? endpoint, out string error)
    {
        endpoint = null;
        error = string.Empty;
        var candidate = (value ?? string.Empty).Trim();

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            error = "أدخل رابط الكمبيوتر الصحيح، مثل http://192.168.1.10:5113";
            return false;
        }

        if (!IsAllowed(uri))
        {
            error = "يُقبل HTTPS، أو HTTP بعنوان محلي للكمبيوتر مثل http://192.168.1.10:5113";
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

    private static bool IsAllowed(Uri uri)
    {
        if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        return IPAddress.TryParse(uri.Host, out var address) && IsPrivateAddress(address);
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return true;

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return address.IsIPv6LinkLocal || (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
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
