using System.IO.Compression;
using System.Threading.Channels;
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
    o.OutPath   = Path.GetFullPath(o.OutPath);
    o.InputPath = Path.GetFullPath(o.InputPath);
    o.Ffmpeg    = string.IsNullOrEmpty(o.Ffmpeg) ? "" : Path.GetFullPath(o.Ffmpeg);

    var osz = o.OutPath + ".osz";

    #region check options

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

    #endregion

    #region parse & convert

    var converter = new Converter(o);

    var bms = Directory
        .GetFiles(o.InputPath, "*.*", SearchOption.AllDirectories)
        .Where(f => availableBmsExt.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

    var skippedFileList      = new List<string>();
    var generationFailedList = new List<string>();

    void Proc(params string[] path)
    {
        try
        {
            converter.Convert(path);
        }
        catch (BmsParserException e)
        {
            skippedFileList.AddRange(e.FailedList);
        }
        catch (GenerationFailedException e)
        {
            generationFailedList.AddRange(e.FailedList);
        }
    }

    Parallel.ForEach(bms.GroupBy(Path.GetDirectoryName), groupedBms =>
    {
        if (o.GenerateMp3)
        {
            Proc(groupedBms.ToArray());
        }
        else
        {
            Parallel.ForEach(groupedBms, g => Proc(g));
        }
    });

    #endregion

    #region after convertion (e.g. remove temp files)

    if (!o.NoCopy)
    {
        logger.Info("Copying files");
        Parallel.ForEach(converter.FilesToCopy, c =>
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

    if (skippedFileList.Any())
    {
        logger.Info(new string('-', 60));
        logger.Info("Skipped List:");

        skippedFileList.ForEach(path => logger.Info(path));
    }

    if (generationFailedList.Any())
    {
        logger.Info(new string('-', 60));
        logger.Info("Generation Failed List:");

        generationFailedList.ForEach(path => logger.Info(path));
    }

    #endregion
});

#region help message

result.WithNotParsed(_ =>
{
    var helpText = HelpText.AutoBuild(result, h =>
    {
        h.AutoHelp                      = true;
        h.AutoVersion                   = false;
        h.AutoVersion                   = false;
        h.AdditionalNewLineAfterOption  = false;
        h.AddNewLineBetweenHelpSections = false;
        h.Heading                       = "";
        h.Copyright                     = "Copyright (c) 2022 QINGQIZ";
        return HelpText.DefaultParsingErrorsHandler(result, h);
    }, e => e);
    Console.WriteLine(helpText);
});

#endregion

#region Help Class

internal class GenerationFailedException : Exception
{
    public readonly List<string> FailedList;

    public GenerationFailedException(IEnumerable<string> generationFailedList)
    {
        FailedList = generationFailedList.ToList();
    }
}

internal class BmsParserException : GenerationFailedException
{
    public BmsParserException(IEnumerable<string> generationFailedList) : base(generationFailedList)
    {
    }
}

internal class Converter
{
    private readonly Option _option;
    private readonly SampleToMp3 _mp3Generator;
    private readonly ILog _log;

    public readonly List<Task> Tasks = new();

    public Converter(Option option)
    {
        _option       = option;
        _mp3Generator = new SampleToMp3(_option);
        _log          = LogManager.GetLogger(nameof(Converter));
    }

    public readonly HashSet<string> FilesToCopy = new();

    private void ConvertOne(BmsFileData data, string mp3Path, HashSet<Sample> excludingSounds)
    {
        var bmsDir    = Path.GetDirectoryName(data.BmsPath) ?? "";
        var outputDir = bmsDir.Replace(_option.InputPath, _option.OutPath);

        Directory.CreateDirectory(outputDir);

        foreach (var includePlate in new[] { true, false })
        {
            var (osuBeatmap, ftc) = data.ToOsuBeatMap(excludingSounds, _option.NoSv, mp3Path, includePlate);

            foreach (var c in ftc)
                lock (FilesToCopy)
                    FilesToCopy.Add(Path.Join(bmsDir, c));

            var plate = includePlate ? " (7+1K)" : "";

            var osuName = $"{data.Metadata.Title} - {data.Metadata.Artist} - BMS Converted{plate} - {Path.GetFileNameWithoutExtension(data.BmsPath)}.osu";

            File.WriteAllText(Path.Join(outputDir, osuName.MakeValidFileName()), osuBeatmap);
        }
    }

    public void Convert(string[] bmsFiles)
    {
        bmsFiles = bmsFiles.OrderBy(s => s).ToArray();

        if (!bmsFiles.Any()) return;

        List<Sample>? soundFileList = null;

        var dataList       = new List<BmsFileData>();
        var parseErrorList = new List<string>();

        var workPath = Path.GetDirectoryName(bmsFiles[0])!;

        foreach (var bmsFilePath in bmsFiles)
        {
            _log.Info($"Processing {bmsFilePath}");

            BmsFileData data;

            try
            {
                data = BmsFileData.FromFile(bmsFilePath);
            }
            catch (InvalidDataException)
            {
                parseErrorList.Add(bmsFilePath);
                continue;
            }

            dataList.Add(data);

            if (!_option.GenerateMp3) continue;

            var soundFiles = data.GetSoundFileList();

            soundFileList ??= soundFiles;
            soundFileList =   soundFileList.Intersect(soundFiles, Sample.Comparer).ToList();

            if (soundFileList.Count < soundFiles.Count / 2)
            {
                _log.Error($"{data.BmsPath}: The sampling intersection of the same song is too small, aborting");
                throw new GenerationFailedException(bmsFiles);
            }
        }

        if (!dataList.Any()) return;

        var filename = $"{dataList[0].Metadata.Title} - {dataList[0].Metadata.Artist}.mp3".MakeValidFileName();

        foreach (var data in dataList)
        {
            try
            {
                ConvertOne(data, filename, new HashSet<Sample>(soundFileList ?? new List<Sample>()));
            }
            catch (InvalidDataException)
            {
                parseErrorList.Add(data.BmsPath);
            }
        }

        var mp3 = Path.Join(
            Path.GetDirectoryName(dataList[0].BmsPath)!
                .Replace(_option.InputPath, _option.OutPath)
          , filename
        );

        try
        {
            if (File.Exists(mp3))
            {
                _log.Warn($"{workPath}: {mp3} exists, skipping...");
            }
            else
            {
                _mp3Generator.GenerateMp3(soundFileList!, workPath, mp3);
            }
        }
        catch
        {
            throw new GenerationFailedException(bmsFiles);
        }

        if (parseErrorList.Any())
        {
            throw new BmsParserException(parseErrorList);
        }
    }
}

#endregion