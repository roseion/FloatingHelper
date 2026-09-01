using FloatingHelper.Core.Actions;

namespace FloatingHelper.Core.Tests;

public class SearchUrlBuilderTests
{
    [Fact]
    public void BuildSearchUrl_ShouldEncodeSpaces()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("hello world");
        Assert.Equal("https://www.bing.com/search?q=hello%20world", url);
    }

    [Fact]
    public void BuildSearchUrl_ShouldEncodeNonAscii()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("测试");
        Assert.StartsWith("https://www.bing.com/search?q=", url);
        Assert.DoesNotContain("测试", url);
    }

    [Fact]
    public void BuildSearchUrl_ShouldTrimQuery()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("  hello  ");
        Assert.EndsWith("q=hello", url);
    }

    [Fact]
    public void BuildSearchUrl_Blank_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => SearchUrlBuilder.BuildSearchUrl("   "));
    }

    [Fact]
    public void BuildSearchUrl_WithCustomTemplate_ShouldFormat()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("hello", "https://example.com/search?q={0}");
        Assert.Equal("https://example.com/search?q=hello", url);
    }

    [Fact]
    public void BuildSearchUrl_BlankTemplate_ShouldUseDefault()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("hello", "   ");
        Assert.Equal("https://www.bing.com/search?q=hello", url);
    }

    [Fact]
    public void BuildSearchUrl_CustomTemplate_ShouldStillEncode()
    {
        var url = SearchUrlBuilder.BuildSearchUrl("a b", "https://example.com/?q={0}");
        Assert.Equal("https://example.com/?q=a%20b", url);
    }
}
