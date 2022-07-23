using System.IO.Compression;
using BmsToOsu;
using BmsToOsu.Converter;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using CommandLine;
using CommandLine.Text;
using log4net;
using SearchOption = System.IO.SearchOption;


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

    if (o.NoCopy && o.GenerateMp3)
    {
        logger.Error($"`--no-copy` is conflict with `--generate-mp3`");
        return;
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

    var converter = new Converter(o.Ffmpeg);

    var ftc = new HashSet<string>();

    var bms = Directory
        .GetFiles(o.InputPath, "*.*", SearchOption.AllDirectories)
        .Where(f => availableBmsExt.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

    void Proc(string path)
    {
        logger.Info($"Processing {path}");
        converter.Convert(path, o, ftc);
    }

    Parallel.ForEach(bms.GroupBy(Path.GetDirectoryName), groupedBms =>
    {
        if (o.GenerateMp3)
        {
            foreach (var fp in groupedBms) Proc(fp);
        }
        else
        {
            Parallel.ForEach(groupedBms, Proc);
        }
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

        if (o.OutPath.EndsWith(".osz", StringComparison.OrdinalIgnoreCase))
        {
            File.Move(osz, o.OutPath);
        }
    }
});

result.WithNotParsed(_ =>
{
    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AutoHelp    = true;
        h.AutoVersion = false;
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);
    Console.WriteLine(helpText);
});


internal class Converter
{
    public Converter(string ffmpeg)
    {
        _mp3Generator = new SampleToMp3(ffmpeg);
        _log          = LogManager.GetLogger(nameof(Converter));
    }

    private readonly SampleToMp3 _mp3Generator;
    private readonly ILog _log;

    private string GenerateMp3(BmsFileData data, string outDir)
    {
        var soundFileList = string.Join('\n', data.GetSoundFileList().Select(x => $"{x.SoundFile}:{x.StartTime:F3}"));

        var otherSoundFileList = Directory.GetFiles(outDir, "*.sound_list");
        foreach (var f in otherSoundFileList)
        {
            if (File.ReadAllText(f).Equals(soundFileList, StringComparison.OrdinalIgnoreCase))
            {
                _log.Warn($"{data.BmsPath}: found duplicate generation task, skipping...");
                return Path.GetFileNameWithoutExtension(f);
            }
        }

        var count    = otherSoundFileList.Length;
        var filename = $"{data.Metadata.Title} - {data.Metadata.Artist}{(count == 0 ? "" : $" ({count})")}.mp3".MakeValidFileName();
        var filePath = Path.Join(outDir, filename);

        if (File.Exists(filePath))
        {
            _log.Warn($"{data.BmsPath}: {filename} exists, skipping...");
            return filename;
        }

        _mp3Generator.GenerateMp3(data, filePath);

        File.WriteAllText(filePath + ".sound_list", soundFileList);

        return filename;
    }

    public void Convert(string bmsFilePath, Option option, HashSet<string> filesToCopy)
    {
        var bmsDir    = Path.GetDirectoryName(bmsFilePath) ?? "";
        var outputDir = bmsDir.Replace(option.InputPath, option.OutPath);

        Directory.CreateDirectory(outputDir);

        BmsFileData data;

        try
        {
            data = BmsFileData.FromFile(bmsFilePath);
        }
        catch (InvalidDataException)
        {
            return;
        }

        foreach (var includePlate in new[] { true, false })
        {
            var mp3Path = option.GenerateMp3 ? GenerateMp3(data, outputDir) : "";

            var (osu, ftc2) = data.ToOsuBeatMap(bmsDir, mp3Path, includePlate);

            lock (filesToCopy)
            {
                foreach (var c in ftc2) filesToCopy.Add(Path.Join(bmsDir, c));
            }

            var plate = includePlate ? " (7+1K)" : "";

            var osuName =
                $"{data.Metadata.Title} - {data.Metadata.Artist} - BMS Converted{plate} - {Path.GetFileNameWithoutExtension(bmsFilePath)}.osu";

            File.WriteAllText(Path.Join(outputDir, osuName.MakeValidFileName()), osu);
        }
    }
}