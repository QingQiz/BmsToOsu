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

    Console.WriteLine("Press Any Key To Exit");
    Console.ReadKey();
}