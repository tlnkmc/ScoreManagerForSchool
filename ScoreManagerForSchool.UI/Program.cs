using Avalonia;
using System;
using ScoreManagerForSchool.Core.Logging;
using ScoreManagerForSchool.UI.Services;

namespace ScoreManagerForSchool.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // 初始化日志系统
            Logger.Initialize();
            Logger.LogInfo("应用程序开始启动", "Program.Main");

            // 设置全局异常处理
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 启动应用程序
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Logger.LogError("应用程序启动失败", "Program.Main", ex);
            ErrorHandler.HandleError(ex, "应用程序启动失败，请查看日志文件获取详细信息。", "Program.Main");
            throw;
        }
        finally
        {
            Logger.LogApplicationShutdown();
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Logger.LogError("未处理的异常", "AppDomain", exception);
            ErrorHandler.HandleError(exception, "应用程序遇到了未处理的错误。", "AppDomain");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
