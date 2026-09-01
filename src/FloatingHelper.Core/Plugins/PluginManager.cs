using System.IO;
using System.Reflection;

namespace FloatingHelper.Core.Plugins;

/// <summary>
/// 插件管理器：维护插件列表，支持内置插件注册、插件目录动态加载、启停与选区适配过滤。
/// </summary>
public sealed class PluginManager : IDisposable
{
    private readonly List<IPlugin> _plugins = new();
    private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>当前已注册的插件列表（含启用与禁用）。</summary>
    public IReadOnlyList<IPlugin> Plugins => _plugins;

    /// <summary>注册内置插件（默认启用）。重复 Id 会被忽略。</summary>
    public bool AddBuiltin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (!_ids.Add(plugin.Id))
        {
            return false;
        }
        plugin.IsEnabled = true;
        _plugins.Add(plugin);
        return true;
    }

    /// <summary>
    /// 从指定目录扫描并加载实现了 <see cref="IPlugin"/> 的程序集。
    /// 单个程序集加载失败会被跳过，不影响其他插件。
    /// </summary>
    /// <returns>成功加载的插件数量。</returns>
    public int LoadFromDirectory(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            return 0;
        }

        var loaded = 0;
        foreach (var dll in Directory.EnumerateFiles(pluginDirectory, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                foreach (var type in GetPluginTypes(assembly))
                {
                    if (Activator.CreateInstance(type) is IPlugin plugin && AddBuiltin(plugin))
                    {
                        loaded++;
                    }
                }
            }
            catch
            {
                // 跳过无法加载或损坏的程序集，避免单个插件拖垮整体。
            }
        }

        return loaded;
    }

    /// <summary>启用 / 禁用指定插件。</summary>
    public void SetEnabled(IPlugin plugin, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        plugin.IsEnabled = enabled;
    }

    /// <summary>从管理器移除并禁用插件。</summary>
    public bool Unload(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        plugin.IsEnabled = false;
        if (_plugins.Remove(plugin))
        {
            _ids.Remove(plugin.Id);
            return true;
        }
        return false;
    }

    /// <summary>返回适配当前选区且处于启用状态的插件。</summary>
    public IReadOnlyList<IPlugin> GetApplicablePlugins(PluginContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _plugins.Where(p => p.IsEnabled && p.CanHandle(context)).ToList();
    }

    public void Dispose()
    {
        _plugins.Clear();
        _ids.Clear();
    }

    private static IEnumerable<Type> GetPluginTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface || !typeof(IPlugin).IsAssignableFrom(type))
            {
                continue;
            }
            yield return type;
        }
    }
}
