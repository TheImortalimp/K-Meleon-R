namespace KMeleonR.Search.Tests;

public class SearchNavigationTests
{
    public static IEnumerable<object[]> Queries()
    {
        foreach (var provider in SearchNavigation.Providers)
        foreach (var query in new[]
        {
            "ordinary search", "explain https://example.com/a?x=1&y=2",
            "why\tdoes.example.fail", "C# & C++ 100% #1 ? x=y+z",
            "caf\u00e9 \u65e5\u672c\u8a9e \U0001f30d", "me@example.com",
            "localhostology", new string('x', 4096) + " full query"
        })
            yield return new object[] { provider.Name, query };
    }

    [Theory]
    [MemberData(nameof(Queries))]
    public void QueryIsSentInFullToOnlyTheSelectedProvider(string name, string query)
    {
        var provider = SearchNavigation.Providers.Single(p => p.Name == name);
        var result = SearchNavigation.Resolve("  " + query + "  ", provider);
        Assert.Equal(provider.Search(query), result);
        Assert.Equal(query, DecodeQuery(result!));
    }

    [Theory]
    [InlineData("Bing", "https://www.bing.com/search?q=foo.bar")]
    [InlineData("Google", "https://www.google.com/search?q=foo.bar")]
    [InlineData("Copilot", "https://www.bing.com/copilotsearch?q=foo.bar")]
    [InlineData("Gemini", "https://www.google.com/search?udm=50&q=foo.bar")]
    public void ExplicitSearchAlwaysPassesDottedText(string name, string expected)
    {
        var provider = SearchNavigation.Providers.Single(p => p.Name == name);
        Assert.Equal(expected, provider.Search("foo.bar"));
        Assert.Equal("https://example.com/a?x=1&y=2",
            DecodeQuery(provider.Search("https://example.com/a?x=1&y=2")));
    }

    [Theory]
    [InlineData("https://example.com/path?q=one#two", "https://example.com/path?q=one#two")]
    [InlineData(" HTTP://EXAMPLE.COM ", "http://example.com/")]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("foo.bar", "https://foo.bar/")]
    [InlineData("example.com/path?q=one#two", "https://example.com/path?q=one#two")]
    [InlineData("localhost:8080/test", "https://localhost:8080/test")]
    [InlineData("127.0.0.1:8080", "https://127.0.0.1:8080/")]
    [InlineData("[::1]:8080", "https://[::1]:8080/")]
    [InlineData("about:blank", "about:blank")]
    public void WebsiteAddressesNavigateRegardlessOfProvider(string text, string expected)
    {
        foreach (var provider in SearchNavigation.Providers)
            Assert.Equal(expected, SearchNavigation.Resolve(text, provider));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public void EmptyAddressSubmissionDoesNothingButAskOpensProviderHome(string? text)
    {
        foreach (var provider in SearchNavigation.Providers)
        {
            Assert.Null(SearchNavigation.Resolve(text, provider));
            Assert.Equal(provider.HomeUrl, provider.Search(text));
        }
    }

    [Theory]
    [InlineData("https://kmr.start/index.html", true)]
    [InlineData("https://kmr.start/index.html?view=start#top", true)]
    [InlineData("https://kmr.start/index.html.evil", false)]
    [InlineData("https://kmr.start/index.html/elsewhere", false)]
    [InlineData("https://kmr.start.evil/index.html", false)]
    [InlineData("https://user@kmr.start/index.html", false)]
    [InlineData("https://kmr.start:444/index.html", false)]
    [InlineData("http://kmr.start/index.html", false)]
    [InlineData("https://www.google.com/search?udm=50&q=hello", false)]
    [InlineData("https://www.bing.com/copilotsearch?q=hello", false)]
    [InlineData(null, false)]
    public void OnlyTheExactLocalStartPageMaySynchronizeSettings(string? source, bool expected)
    {
        Assert.Equal(expected, SearchNavigation.IsStartPage(source));
    }

    private static string DecodeQuery(string url) =>
        Uri.UnescapeDataString(new Uri(url).Query.Split("q=", 2)[1]);
}
