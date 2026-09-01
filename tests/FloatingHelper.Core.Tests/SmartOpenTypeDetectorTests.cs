using FloatingHelper.Core.Actions;

namespace FloatingHelper.Core.Tests;

public class SmartOpenTypeDetectorTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://www.example.com/a/b?x=1&y=2")]
    [InlineData("www.example.com")]
    public void Detect_Url_ShouldReturnUrl(string text)
    {
        Assert.Equal(OpenTargetType.Url, SmartOpenTypeDetector.Detect(text));
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.example.co.uk")]
    public void Detect_Email_ShouldReturnEmail(string text)
    {
        Assert.Equal(OpenTargetType.Email, SmartOpenTypeDetector.Detect(text));
    }

    [Fact]
    public void Detect_ExistingDirectory_ShouldReturnFilePath()
    {
        Assert.Equal(OpenTargetType.FilePath, SmartOpenTypeDetector.Detect(AppContext.BaseDirectory));
    }

    [Fact]
    public void Detect_ExistingFile_ShouldReturnFilePath()
    {
        var file = typeof(SmartOpenTypeDetectorTests).Assembly.Location;
        Assert.Equal(OpenTargetType.FilePath, SmartOpenTypeDetector.Detect(file));
    }

    [Fact]
    public void Detect_PlainText_ShouldReturnPlainText()
    {
        Assert.Equal(OpenTargetType.PlainText, SmartOpenTypeDetector.Detect("这是一段普通文本"));
    }

    [Fact]
    public void Detect_NonexistentPath_ShouldReturnPlainText()
    {
        var fake = Path.Combine(Path.GetTempPath(), "NoSuchDir_123456", "file.txt");
        Assert.Equal(OpenTargetType.PlainText, SmartOpenTypeDetector.Detect(fake));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_Blank_ShouldReturnPlainText(string? text)
    {
        Assert.Equal(OpenTargetType.PlainText, SmartOpenTypeDetector.Detect(text));
    }

    [Theory]
    [InlineData(@"D:\some\path")]
    [InlineData(@"C:\Users\me")]
    public void IsLikelyPath_WindowsDrive_ShouldBeTrue(string text)
    {
        Assert.True(SmartOpenTypeDetector.IsLikelyPath(text));
    }

    [Fact]
    public void IsLikelyPath_PlainText_ShouldBeFalse()
    {
        Assert.False(SmartOpenTypeDetector.IsLikelyPath("普通文本"));
    }
}
