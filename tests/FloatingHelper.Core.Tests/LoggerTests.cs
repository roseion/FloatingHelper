using FloatingHelper.Core.Logging;

namespace FloatingHelper.Core.Tests;

public class LoggerTests
{
    [Fact]
    public void Info_ShouldNotThrow()
    {
        Logger.EnsureInitialized();
        Logger.Info("test-info-" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Warn_ShouldNotThrow()
    {
        Logger.EnsureInitialized();
        Logger.Warn("test-warn-" + Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Error_WithException_ShouldNotThrow()
    {
        Logger.EnsureInitialized();
        Logger.Error("test-error", new InvalidOperationException("boom"));
    }

    [Fact]
    public void EnsureInitialized_MultipleCalls_ShouldNotThrow()
    {
        Logger.EnsureInitialized();
        Logger.EnsureInitialized();
        Logger.Info("multi-init-safe");
    }
}
