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
}
