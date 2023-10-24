using NLog;

namespace BmsToOsu;

public static class Logger
{
    public static void Config()
    {
        NLog.LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger()
                .FilterMinLevel(LogLevel.Info)
                .WriteToConsole("|${level}|${message}").WithAsync();
        });
    }
}