namespace FloatingHelper.Core.Actions;

/// <summary>
/// 构造「搜索」动作的默认搜索引擎 URL。
/// </summary>
public static class SearchUrlBuilder
{
    /// <summary>默认搜索引擎地址模板，{0} 为编码后的查询词。</summary>
    public const string DefaultSearchTemplate = "https://www.bing.com/search?q={0}";

    /// <summary>
    /// 构造搜索 URL。查询词会做 URI 编码。
    /// </summary>
    public static string BuildSearchUrl(string query, string? template = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("查询词不能为空。", nameof(query));
        }

        var encoded = Uri.EscapeDataString(query.Trim());
        var format = string.IsNullOrWhiteSpace(template) ? DefaultSearchTemplate : template;
        return string.Format(format, encoded);
    }
}
