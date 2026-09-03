using FloatingHelper.Core.Actions;

namespace FloatingHelper.Core.Configuration;

/// <summary>
/// 应用配置模型：搜索模板、插件启停与外部路径、开机自启等持久化配置。
/// </summary>
public sealed class AppSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>搜索模板，{0} 为 URI 编码后的查询词。</summary>
    public string SearchTemplate { get; set; } = SearchUrlBuilder.DefaultSearchTemplate;

    /// <summary>插件配置（键为插件 Id）。</summary>
    public Dictionary<string, PluginSetting> Plugins { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>是否开机自启。</summary>
    public bool AutoStart { get; set; }

    /// <summary>工具栏按钮显示模式。</summary>
    public ToolbarDisplayMode DisplayMode { get; set; } = ToolbarDisplayMode.IconAndText;
}

/// <summary>工具栏按钮显示模式。</summary>
public enum ToolbarDisplayMode
{
    /// <summary>同时显示图标与文字。</summary>
    IconAndText = 0,

    /// <summary>仅显示图标，鼠标悬停弹出文字提示。</summary>
    IconOnly = 1,

    /// <summary>仅显示文字。</summary>
    TextOnly = 2,
}

/// <summary>单个插件的持久化配置。</summary>
public sealed class PluginSetting
{
    /// <summary>插件是否启用。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>外部插件程序集路径；内置插件为 null。</summary>
    public string? Path { get; set; }
}
