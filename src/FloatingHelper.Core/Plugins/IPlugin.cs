using System.Windows;

namespace FloatingHelper.Core.Plugins;

/// <summary>
/// 插件统一接口。所有工具栏动作（复制 / 智能打开 / 搜索 及后续扩展）都以插件形式实现。
/// </summary>
public interface IPlugin
{
    /// <summary>全局唯一的插件标识。</summary>
    string Id { get; }

    /// <summary>展示给用户的插件名称。</summary>
    string Name { get; }

    /// <summary>插件说明。</summary>
    string Description { get; }

    /// <summary>插件是否启用。禁用后工具栏不再展示该动作。</summary>
    bool IsEnabled { get; set; }

    /// <summary>插件是否提供设置界面。默认 false，不支持设置的插件无需重写。</summary>
    bool HasSettings => false;

    /// <summary>判断当前选区是否适配本插件（决定工具栏是否显示该动作）。</summary>
    bool CanHandle(PluginContext context);

    /// <summary>执行插件动作。返回执行结果描述，null 表示无附加结果。</summary>
    Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// 打开插件设置窗口。仅在 HasSettings=true 时被主程序调用。
    /// 默认空实现，支持设置的插件需重写此方法并弹出自己的设置界面。
    /// 插件应自行管理配置持久化（建议 %AppData%\FloatingHelper\plugins\{Id}.json）。
    /// </summary>
    void ShowSettings(Window? owner = null) { }
}
