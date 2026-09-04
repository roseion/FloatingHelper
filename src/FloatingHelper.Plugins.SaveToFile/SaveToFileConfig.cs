namespace FloatingHelper.Plugins.SaveToFile;

/// <summary>「保存」插件配置：要保存到的目标文本文档路径。</summary>
public sealed class SaveToFileConfig
{
    /// <summary>目标文本文档的完整路径；未设置时为 null。</summary>
    public string? TargetFilePath { get; set; }
}
