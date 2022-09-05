using BmsToOsu;
using BmsToOsu.Launcher;


Logger.Config();

if (args.Any())
{
    CliArgsLauncher.Launch(args);
}
else
{
    GuiLauncher.Launch();
}