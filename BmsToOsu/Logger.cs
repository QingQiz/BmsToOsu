using System.Reflection;
using NLog;
using NLog.Conditions;
using NLog.Targets;

namespace BmsToOsu;

public static class Logger
{
    public static void Config()
    {
        LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger()
                .FilterMinLevel(LogLevel.Info)
                .WriteTo(new ColoredConsoleTarget()
                {
                    Layout = "|${level}|${message}",
                    RowHighlightingRules =
                    {
                        new ConsoleRowHighlightingRule(
                            ConditionParser.ParseExpression("level == LogLevel.Info"),
                            ConsoleOutputColor.Gray, ConsoleOutputColor.NoChange),
                        new ConsoleRowHighlightingRule(
                            ConditionParser.ParseExpression("level == LogLevel.Warn"), 
                            ConsoleOutputColor.Yellow, ConsoleOutputColor.NoChange),
                        new ConsoleRowHighlightingRule(
                            ConditionParser.ParseExpression("level == LogLevel.Error"), 
                            ConsoleOutputColor.Red, ConsoleOutputColor.NoChange),
                        new ConsoleRowHighlightingRule(
                            ConditionParser.ParseExpression("level == LogLevel.Fatal"), 
                            ConsoleOutputColor.White, ConsoleOutputColor.Red)
                    }
                })
                .WriteToFile(Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),  "log.txt"))
                .WithAsync();
        });
    }
}