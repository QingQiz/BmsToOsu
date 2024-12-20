using System.Diagnostics;
using System.Text;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using NLog;

namespace BmsToOsu.Converter;

public class SampleToMp3
{
    private readonly string _ffmpeg;
    private readonly Option _option;

    private readonly ILogger _log = LogManager.GetCurrentClassLogger();
    private readonly SemaphoreSlim _lock;

    private readonly AudioValidator _validator;

    public SampleToMp3(Option option)
    {
        _option = option;
        var ffmpeg = _option.Ffmpeg;

        _lock = new SemaphoreSlim(_option.MaxThreads, _option.MaxThreads);

        if (string.IsNullOrEmpty(ffmpeg))
        {
            foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                var p = Path.Join(path, "ffmpeg");

                if (File.Exists(p))
                {
                    _ffmpeg = p;
                    break;
                }

                if (File.Exists(p + ".exe"))
                {
                    _ffmpeg = p + ".exe";
                    break;
                }
            }

            if (string.IsNullOrEmpty(_ffmpeg))
            {
                throw new FileNotFoundException("Can not find ffmpeg in PATH, use `--ffmpeg` to specify the path of ffmpeg");
            }
            
            _log.Info($"Use FFMPEG: {_ffmpeg}");
        }
        else
        {
            _ffmpeg = ffmpeg;
        }

        _validator = new AudioValidator(_ffmpeg);
    }

    private void Generate(List<Sample> samples, string workPath, string output)
    {
        var filter = new FilterComplex();

        foreach (var sample in samples)
        {
            filter.AddFile(sample.SoundFile, sample.StartTime);
        }

        var argsFile = Path.GetTempPath() + Guid.NewGuid() + ".txt";

        File.WriteAllText(argsFile, filter.GetScript());

        using var p = new Process();

        p.StartInfo.UseShellExecute  = false;
        p.StartInfo.CreateNoWindow   = false;
        p.StartInfo.WorkingDirectory = workPath;
        p.StartInfo.FileName         = _ffmpeg;
        p.StartInfo.Arguments =
            $"-y -hide_banner -loglevel error -filter_complex_script \"{argsFile}\" -map \"[mix]\" -b:a 256k \"{output}\"";

        // Prevent performance degradation due to too many threads
        _lock.Wait();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            p.Start();
            p.WaitForExit();
        }
        finally
        {
            _lock.Release();
        }

        if (p.ExitCode != 0)
        {
            throw new SampleRemixException("remix failed");
        }

        File.Delete(argsFile);
    }

    public HashSet<string> GenerateMp3(List<Sample> samples, string workPath, string output)
    {
        var invalid = _validator.CheckSoundValidity(
            samples.Select(s => s.SoundFile).ToList(), workPath
        ).Result.ToHashSet();

        foreach (var sample in invalid)
        {
            _log.Warn($"Invalid Sound File: {Path.Join(workPath, sample)}, ignoring...");
        }

        samples = samples.Where(s => !invalid.Contains(s.SoundFile)).ToList();

        if (samples.Count < Constants.MinSoundFileCount)
        {
            _log.Fatal($"{workPath}: too few valid audio files, aborting...");
            throw new SampleSetTooSmallException();
        }

        var groupSize = Math.Max(
            Math.Min((samples.Count + _option.MaxThreads - 1) / _option.MaxThreads, Constants.MaxFileCountFfmpegCanRead)
          , 10
        );

        var groupedSamples = new List<(List<Sample> Samples, string Output)>();

        var n = 0;

        // group sound file list by every X elements
        while (true)
        {
            var l = samples.Skip(n++ * groupSize).Take(groupSize).ToList();
            if (!l.Any()) break;

            groupedSamples.Add((l, Path.GetTempPath() + Guid.NewGuid() + ".mp3"));
        }

        // generate mp3 in parallel
        Parallel.ForEach(groupedSamples, g =>
        {
            var startTime = g.Samples.Min(x => x.StartTime);
            var endTime   = g.Samples.Max(x => x.StartTime);

            _log.Info(
                $"{workPath}: Generating {TimeSpan.FromMilliseconds(startTime)}-{TimeSpan.FromMilliseconds(endTime)}...");

            Generate(g.Samples, workPath, g.Output);
        });

        // merge temp mp3 files to result
        _log.Info($"{workPath}: merging...");

        samples = groupedSamples.Select(x => new Sample(0d, x.Output)).ToList();
        Generate(samples, workPath, output);
        samples.ForEach(l => File.Delete(l.SoundFile));

        return invalid;
    }
}

internal class FilterComplex
{
    private readonly StringBuilder _inputBuilder = new();
    private readonly StringBuilder _delayBuilder = new();
    private readonly StringBuilder _mixBuilder = new();

    private int _fileCount;

    private static readonly List<(string, string)> Escape = new()
    {
        (@"\", @"\\\\"),
        (@"=", @"\\\="),
        (@",", @"\,"),
        (@";", @"\;"),
        (@"[", @"\["),
        (@"]", @"\]"),
        (@":", @"\\\:"),
        (@"'", @"\\\'")
    };

    private static string EscapeWindowsPath(string p)
    {
        return Escape.Aggregate(p, (current, e) => current.Replace(e.Item1, e.Item2));
    }

    public void AddFile(string path, double delay)
    {
        if (string.IsNullOrEmpty(path)) return;

        _inputBuilder.AppendLine($"amovie={EscapeWindowsPath(path)}[input_{_fileCount}];");
        _delayBuilder.AppendLine($"[input_{_fileCount}]adelay={delay:F3}|{delay:F3}[delay_{_fileCount}];");
        _mixBuilder.Append($"[delay_{_fileCount}]");
        _fileCount++;
    }

    public string GetScript()
    {
        return $"{_inputBuilder}{_delayBuilder}{_mixBuilder}amix=inputs={_fileCount}:normalize=0[mix]";
    }
}