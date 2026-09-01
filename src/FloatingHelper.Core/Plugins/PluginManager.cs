using System.IO;
using System.Reflection;

namespace FloatingHelper.Core.Plugins;

/// <summary>
/// 插件管理器：维护插件列表，支持内置插件注册、插件目录/单文件动态加载、启停、卸载与选区适配过滤。
/// </summary>
public sealed class PluginManager : IDisposable
{
    private readonly List<IPlugin> _plugins = new();
    private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _externalPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>当前已注册的插件列表（含启用与禁用）。</summary>
    public IReadOnlyList<IPlugin> Plugins => _plugins;

    /// <summary>插件状态变更（新增 / 卸载 / 启停）时触发，供配置持久化挂接。</summary>
    public event EventHandler? StateChanged;

    /// <summary>注册内置插件（默认启用）。重复 Id 会被忽略。</summary>
    public bool AddBuiltin(IPlugin plugin) => AddPlugin(plugin);

    /// <summary>是否为内置插件（非外部 DLL 加载）。</summary>
    public bool IsBuiltin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return !_externalPaths.ContainsKey(plugin.Id);
    }

    /// <summary>获取外部插件的程序集路径；内置插件返回 null。</summary>
    public string? GetExternalPath(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        return _externalPaths.TryGetValue(plugin.Id, out var path) ? path : null;
    }

    /// <summary>按 Id 查询插件；不存在返回 null。</summary>
    public IPlugin? GetPlugin(string id) =>
        _plugins.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

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
            loaded += LoadAssembly(dll);
        }

        return loaded;
    }

    /// <summary>加载单个外部插件程序集文件（DLL）。文件不存在或加载失败返回 0。</summary>
    public int LoadFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return 0;
        }

        return LoadAssembly(filePath);
    }

    /// <summary>启用 / 禁用指定插件。</summary>
    public void SetEnabled(IPlugin plugin, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (plugin.IsEnabled == enabled)
        {
            return;
        }

        plugin.IsEnabled = enabled;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 卸载插件。内置插件不可卸载（仅可启停），返回 false；外部插件卸载成功返回 true。
    /// </summary>
    public bool Unload(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        // 内置插件不可卸载，仅可启停。
        if (IsBuiltin(plugin))
        {
            return false;
        }

        plugin.IsEnabled = false;
        if (_plugins.Remove(plugin))
        {
            _ids.Remove(plugin.Id);
            _externalPaths.Remove(plugin.Id);
            StateChanged?.Invoke(this, EventArgs.Empty);
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
        _externalPaths.Clear();
    }

    private bool AddPlugin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        if (!_ids.Add(plugin.Id))
        {
            return false;
        }

        plugin.IsEnabled = true;
        _plugins.Add(plugin);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private int LoadAssembly(string path)
    {
        try
        {
            var assembly = Assembly.LoadFrom(path);
            var loaded = 0;
            foreach (var type in GetPluginTypes(assembly))
            {
                if (Activator.CreateInstance(type) is IPlugin plugin && AddPlugin(plugin))
                {
                    _externalPaths[plugin.Id] = path;
                    loaded++;
                }
            }

            return loaded;
        }
        catch
        {
            // 跳过无法加载或损坏的程序集，避免单个插件拖垮整体。
            return 0;
        }
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
