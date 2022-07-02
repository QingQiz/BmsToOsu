using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace BmsToOsu;

public static class Logger
{
    public static void Config()
    {
        var hierarchy = (Hierarchy)LogManager.GetRepository();

        var patternLayout = new PatternLayout
        {
            ConversionPattern = "[%level] %message%newline"
        };

        patternLayout.ActivateOptions();

        var coloredConsoleAppender = new ColoredConsoleAppender();

        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {
            BackColor = ColoredConsoleAppender.Colors.Red,
            ForeColor = ColoredConsoleAppender.Colors.White,
            Level     = Level.Error
        });

        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {
            ForeColor = ColoredConsoleAppender.Colors.Yellow,
            Level     = Level.Warn
        });

        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {
            ForeColor = ColoredConsoleAppender.Colors.Red,
            Level     = Level.Fatal
        });

        coloredConsoleAppender.AddMapping(new ColoredConsoleAppender.LevelColors
        {
            ForeColor = ColoredConsoleAppender.Colors.Green,
            Level     = Level.Debug
        });

        coloredConsoleAppender.Layout = patternLayout;

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        coloredConsoleAppender.ActivateOptions();

        hierarchy.Root.AddAppender(coloredConsoleAppender);
        hierarchy.Root.Level = Level.Info;
        hierarchy.Configured = true;
    }
}