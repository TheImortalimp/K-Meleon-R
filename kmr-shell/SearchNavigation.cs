#nullable enable

namespace KMeleonR;

internal sealed record SearchProvider(string Name, string Url, string HomeUrl)
{
    public string Search(string? text)
    {
        var query = text?.Trim() ?? "";
        return query.Length == 0 ? HomeUrl : string.Format(Url, Uri.EscapeDataString(query));
    }
}

internal static class SearchNavigation
{
    public const string StartUrl = "https://kmr.start/index.html";

    public static readonly SearchProvider[] Providers =
    {
        new("Bing", "https://www.bing.com/search?q={0}", "https://www.bing.com/"),
        new("Google", "https://www.google.com/search?q={0}", "https://www.google.com/"),
        new("Copilot", "https://www.bing.com/copilotsearch?q={0}", "https://www.bing.com/copilotsearch"),
        new("Gemini", "https://www.google.com/search?udm=50&q={0}", "https://gemini.google.com/app"),
    };

    public static string? Resolve(string? text, SearchProvider provider) =>
        string.IsNullOrWhiteSpace(text) ? null : Normalize(text) ?? provider.Search(text);

    private static string? Normalize(string text)
    {
        var input = text.Trim();
        // A question containing a URL is still a query, not an address.
        if (input.Any(char.IsWhiteSpace)) return null;
        if ((input.Contains("://") || input.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) &&
            Uri.TryCreate(input, UriKind.Absolute, out var absolute))
            return absolute.AbsoluteUri;

        if (!Uri.TryCreate("https://" + input, UriKind.Absolute, out var address) ||
            address.UserInfo.Length != 0)
            return null;

        return address.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6 ||
            address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            (address.HostNameType == UriHostNameType.Dns && address.Host.Contains('.'))
            ? address.AbsoluteUri
            : null;
    }

    public static bool IsStartPage(string? source) =>
        Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.Host == "kmr.start" &&
        uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.AbsolutePath == "/index.html";
}
