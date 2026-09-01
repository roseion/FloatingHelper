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

    /// <summary>判断当前选区是否适配本插件（决定工具栏是否显示该动作）。</summary>
    bool CanHandle(PluginContext context);

    /// <summary>执行插件动作。返回执行结果描述，null 表示无附加结果。</summary>
    Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);
}
