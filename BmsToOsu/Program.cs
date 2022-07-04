using System.IO.Compression;
using BmsToOsu;
using BmsToOsu.Converter;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using CommandLine;
using CommandLine.Text;
using log4net;

Logger.Config();

var logger = LogManager.GetLogger("");

var availableBmsExt = new[]
{
    ".bms", ".bml", ".bme", ".bmx"
};

var parser = new Parser(with =>
{
    with.AutoVersion = false;
    with.AutoHelp    = true;
    with.HelpWriter  = null;
});

var result = parser.ParseArguments<Option>(args);

result.WithParsed(o =>
{
    var osz = o.OutPath + ".osz";

    // avoid removing existing folder
    if (Directory.Exists(o.OutPath) && !o.NoRemove)
    {
        logger.Warn($"{o.OutPath} exists, `--no-remove` will be appended to the parameter");
        o.NoRemove = true;
    }

    // avoid removing after generation
    if (o.NoZip && !o.NoRemove)
    {
        logger.Warn("`--no-remove` is appended to the parameter");
        o.NoRemove = true;
    }

    // avoid duplication
    if (File.Exists(osz))
    {
        logger.Warn($"{osz} exists, ignoring...");
        return;
    }

    var ftc = new HashSet<string>();

    var bms = Directory
        .GetFiles(o.InputPath, "*.*", SearchOption.AllDirectories)
        .Where(f => availableBmsExt.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

    Parallel.ForEach(bms, fp =>
    {
        logger.Info($"Processing {fp}");

        var dir = Path.GetDirectoryName(fp) ?? "";

        BmsFileData data;

        try
        {
            data = BmsFileData.FromFile(fp);
        }
        catch (InvalidDataException)
        {
            return;
        }

        var (osu, ftc2) = data.ToOsuBeatMap(dir);

        lock (ftc)
        {
            foreach (var c in ftc2) ftc.Add(Path.Join(dir, c));
        }

        var dest = dir.Replace(o.InputPath, o.OutPath);

        var osuName =
            $"{data.Metadata.Title} - {data.Metadata.Artist} - BMS Converted - {Path.GetFileNameWithoutExtension(fp)}.osu";

        Directory.CreateDirectory(dest);
        File.WriteAllText(Path.Join(dest, osuName.MakeValidFileName()), osu);
    });

    if (!o.NoCopy)
    {
        logger.Info("Copying sound files");
        Parallel.ForEach(ftc, c =>
        {
            var dest = c.Replace(o.InputPath, o.OutPath);
            dest = Path.Join(Path.GetDirectoryName(dest), Path.GetFileName(dest).Escape());

            if (!File.Exists(dest))
            {
                File.Copy(c, dest, true);
            }
        });
    }

    if (!o.NoZip && Directory.Exists(o.OutPath))
    {
        logger.Info($"Creating {osz}");
        ZipFile.CreateFromDirectory(o.OutPath, osz, CompressionLevel.Fastest, false);
    }

    if (!o.NoRemove)
    {
        logger.Info($"Removing {o.OutPath}");
        Directory.Delete(o.OutPath, true);
    }
});

result.WithNotParsed(errs =>
{
    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AutoHelp    = true;
        h.AutoVersion = false;
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);
    Console.WriteLine(helpText);
});