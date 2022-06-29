using System.IO.Compression;
using BmsToOsu;
using BmsToOsu.Converter;
using BmsToOsu.Entity;
using CommandLine;
using log4net;

Logger.Config();

var logger = LogManager.GetLogger("");

var availableBmsExt = new[]
{
    ".bms", ".bml", ".bme", ".bmx"
};

Parser.Default.ParseArguments<Option>(args)
    .WithParsed(o =>
    {
        var osz = o.OutPath.EndsWith(".osz", StringComparison.OrdinalIgnoreCase) ? o.OutPath : o.OutPath + ".osz";

        if (File.Exists(osz))
        {
            logger.Warn($"{osz} exists, ignoring...");
            return;
        }

        var ftc = new HashSet<string>();
       
        foreach (var song in Directory.GetDirectories(o.InputPath).Take(o.Number <= 0 ? int.MaxValue : o.Number))
        {
            logger.Info($"Processing {Path.GetFileName(song)}");

            var bms = Directory
                .GetFiles(song, "*.*", SearchOption.AllDirectories)
                .Where(f => availableBmsExt.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

            foreach (var fp in bms)
            {
                try
                {
                    var dir = Path.GetDirectoryName(fp) ?? "";

                    var data = BmsFileData.FromFile(fp);

                    var (osu, ftc2) = data.ToOsuBeatMap();

                    foreach (var c in ftc2) ftc.Add(Path.Join(dir, c));

                    var dest = dir.Replace(o.InputPath, o.OutPath);

                    Directory.CreateDirectory(dest);
                    File.WriteAllText(Path.Join(dest, Path.GetFileNameWithoutExtension(fp) + ".osu"), osu);
                }
                catch (InvalidDataException)
                {
                }
            }
        }

        if (!o.NoCopy)
        {
            Parallel.ForEach(ftc, c =>
            {
                var dest = c.Replace(o.InputPath, o.OutPath);

                if (!File.Exists(dest))
                {
                    File.Copy(c, dest, true);
                }
            });
        }

        if (!o.NoZip && Directory.Exists(osz))
        {
            ZipFile.CreateFromDirectory(o.OutPath, osz, CompressionLevel.Fastest, false);
        }

        if (!o.NoRemove)
        {
            Directory.Delete(o.OutPath, true);
        }
    });