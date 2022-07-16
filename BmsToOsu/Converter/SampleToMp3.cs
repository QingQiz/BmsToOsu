using System.Diagnostics;
using System.Text;
using BmsToOsu.Entity;
using log4net;

namespace BmsToOsu.Converter;

/// <summary>
/// Example
/// <code>
/// var bmsFilePath = @"path/to/name.bms";
/// var data = BmsFileData.FromFile(bmsFilePath);
/// var outPath = @"path/to/output/name.mp3";
/// SampleToMp3.ToMp3(data, outPath);
/// </code>
/// </summary>
public static class SampleToMp3
{
    private class FilterComplex
    {
        private readonly StringBuilder _inputBuilder = new();
        private readonly StringBuilder _delayBuilder = new();
        private readonly StringBuilder _mixBuilder = new();

        private int _fileCount = 0;

        private static string EscapeWindowsPath(string p)
        {
            return $"'{p.Replace(@"\", @"\\").Replace(":", @"\:")}'";
        }

        public void AddFile(string path, double delay)
        {
            _inputBuilder.AppendLine($"amovie={EscapeWindowsPath(path)}[input_{_fileCount}];");
            _delayBuilder.AppendLine($"[input_{_fileCount}]adelay={delay}|{delay}[delay_{_fileCount}];");
            _mixBuilder.Append($"[delay_{_fileCount}]");
            _fileCount++;
        }

        public string GetScript()
        {
            return $"{_inputBuilder}{_delayBuilder}{_mixBuilder}amix=inputs={_fileCount}:normalize=0[mix]";
        }
    }

    private static void Convert(List<(double StartTime, string SoundFile)> soundList, string ffmpeg, string workPath, string output)
    {
        var filter = new FilterComplex();
        foreach (var sound in soundList)
        {
            filter.AddFile(sound.SoundFile, sound.StartTime);
        }

        var argsFile = Path.GetTempPath() + Guid.NewGuid() + ".txt";

        File.WriteAllText(argsFile, filter.GetScript());

        using var p = new Process();

        p.StartInfo.UseShellExecute  = false;
        p.StartInfo.CreateNoWindow   = false;
        p.StartInfo.WorkingDirectory = workPath;
        p.StartInfo.FileName         = ffmpeg;
        p.StartInfo.Arguments =
            $"-y -hide_banner -loglevel error -filter_complex_script \"{argsFile}\" -map \"[mix]\" -b:a 256k \"{output}\"";

        p.Start();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            throw new Exception("convert failed");
        }

        File.Delete(argsFile);
    }

    public static void ToMp3(BmsFileData data, string output)
    {
        var log = LogManager.GetLogger(typeof(SampleToMp3));

        var ffmpeg = "";

        foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            var p = Path.Join(path, "ffmpeg.exe");

            if (!File.Exists(p)) continue;

            ffmpeg = p;
            break;
        }

        if (string.IsNullOrEmpty(ffmpeg))
        {
            throw new FileNotFoundException("Can not find ffmpeg.exe in PATH");
        }

        var allSoundList = new List<(double StartTime, string SoundFile)>();

        allSoundList.AddRange(data.SoundEffects.Select(s => (s.StartTime, s.SoundFile)));
        allSoundList.AddRange(data.HitObject.Values.SelectMany(x => x).Select(x => (x.StartTime, x.HitSoundFile)));
        allSoundList = allSoundList.OrderBy(l => l.StartTime).ToList();

        var groupSize = allSoundList.Count / 10;

        var groupedSoundList = new List<(List<(double StartTime, string SoundFile)> SoundList, string Output)>();

        var n = 0;

        while (true)
        {
            var l = allSoundList.Skip(n++ * groupSize).Take(groupSize).ToList();
            if (!l.Any()) break;

            groupedSoundList.Add((l, Path.GetTempPath() + Guid.NewGuid() + ".mp3"));
        }

        var workPath = Path.GetDirectoryName(data.BmsPath)!;

        Parallel.ForEach(groupedSoundList, g =>
        {
            var startTime = g.SoundList.Min(x => x.StartTime);
            var endTime   = g.SoundList.Max(x => x.StartTime);

            log.Info(
                $"{data.BmsPath}: Converting {TimeSpan.FromMilliseconds(startTime)}-{TimeSpan.FromMilliseconds(endTime)}...");

            Convert(g.SoundList, ffmpeg, workPath, g.Output);
        });

        {
            log.Info($"{data.BmsPath}: merging...");

            var soundList = groupedSoundList.Select(x => (0d, x.Output)).ToList();

            Convert(soundList, ffmpeg, workPath, output);

            soundList.ForEach(l => File.Delete(l.Output));
        }
    }
}