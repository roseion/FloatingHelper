using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>
/// 「用默认浏览器打开指定网站页面」类插件的公共基类。
/// 这类插件的动作模式一致：把选中的文本拼成目标 URL，然后用系统默认浏览器打开。
/// 子类只需提供 Id / Name / Description / Icon / UrlTemplate，个别需要特殊拼接的
/// （如豆包的 JSON 参数）重写 <see cref="BuildUrl"/> 即可。
/// </summary>
public abstract class UrlLaunchPluginBase : IPlugin
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract string Icon { get; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>目标 URL 模板，{0} 为 URL 编码后的选中文本。</summary>
    protected virtual string UrlTemplate => string.Empty;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>
    /// 把选中文本拼成目标 URL。默认按 UrlTemplate 构造；子类可重写以支持特殊拼接。
    /// </summary>
    public virtual string BuildUrl(string text)
        => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public virtual Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(context.SelectedText);
        var ok = ProcessLauncher.Open(url);
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
