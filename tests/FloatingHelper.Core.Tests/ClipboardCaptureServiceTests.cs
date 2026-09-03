using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Selection;

namespace FloatingHelper.Core.Tests;

/// <summary>
/// 剪贴板降级捕获相关测试。剪贴板是进程级全局资源，且依赖 STA 线程，
/// 因此测试在 STA 线程中串行执行，并使用独立 collection 避免与其它测试并行争用剪贴板。
/// </summary>
[Collection("Clipboard")]
public class ClipboardCaptureServiceTests
{
    [Fact]
    public void ReadClipboardText_AfterCopy_ShouldReturnText()
    {
        RunInSta(() =>
        {
            ClipboardHelper.CopyText("hello 世界");
            var text = ClipboardCaptureService.ReadClipboardText();
            Assert.Equal("hello 世界", text);
        });
    }

    [Fact]
    public void SnapshotRestore_ShouldPreserveTextContent()
    {
        RunInSta(() =>
        {
            ClipboardHelper.CopyText("original-content");
            var snapshot = ClipboardCaptureService.CaptureSnapshot();

            ClipboardHelper.CopyText("changed-content");
            Assert.Equal("changed-content", ClipboardCaptureService.ReadClipboardText());

            ClipboardCaptureService.RestoreSnapshot(snapshot);
            Assert.Equal("original-content", ClipboardCaptureService.ReadClipboardText());
        });
    }

    [Fact]
    public void SnapshotRestore_EmptyClipboard_ShouldRestoreEmpty()
    {
        RunInSta(() =>
        {
            // 先清空剪贴板再快照（快照应为空）。
            ClipboardHelper.CopyText(string.Empty);
            var snapshot = ClipboardCaptureService.CaptureSnapshot();

            ClipboardHelper.CopyText("temp");
            ClipboardCaptureService.RestoreSnapshot(snapshot);

            // 空快照恢复后，不应包含文本。
            Assert.Null(ClipboardCaptureService.ReadClipboardText());
        });
    }

    /// <summary>在 STA 线程中执行剪贴板操作，捕获异常并传播。</summary>
    private static void RunInSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA 线程执行失败：{error.Message}");
        }
    }
}
