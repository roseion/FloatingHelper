using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.SiteLauncher;

/// <summary>
/// DeepSeek 提问插件：打开 DeepSeek 网页对话，并带上选中文本（q 参数）。
/// 说明：若浏览器安装了 DeepSeek 社区增强脚本（如 deepseek-prompt-automation），
/// 页面会读取 q 参数自动填入并发送；未安装时则停留在对话页，粘贴后发送即可。
/// </summary>
public sealed class DeepSeekAskPlugin : UrlLaunchPluginBase
{
    public override string Id => "site.ask.deepseek";

    public override string Name => "DeepSeek";

    public override string Icon => "\uE8F1";

    public override string Description => "在浏览器中打开 DeepSeek 对话页并带入选中文本作为提问";

    protected override string UrlTemplate => "https://chat.deepseek.com/a/chat?q={0}";
}
