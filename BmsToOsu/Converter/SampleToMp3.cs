using System.Diagnostics;
using System.Text;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Converter;

public class SampleToMp3
{
    private class FilterComplex
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

    private readonly string _ffmpeg;
    private readonly Option _option;

    private readonly SemaphoreSlim _lock;

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
        }
        else
        {
            _ffmpeg = ffmpeg;
        }

        _log = LogManager.GetLogger(typeof(SampleToMp3));
    }

    private readonly ILog _log;
    private readonly Dictionary<string, bool> _fileValidity = new();

    private bool CheckSoundValidity(string path, string workPath)
    {
        var fullPath = Path.Join(workPath, path);

        lock (_fileValidity)
        {
            if (_fileValidity.ContainsKey(fullPath)) return _fileValidity[fullPath];
        }

        using var p = new Process();

        p.StartInfo.UseShellExecute  = false;
        p.StartInfo.CreateNoWindow   = false;
        p.StartInfo.WorkingDirectory = workPath;
        p.StartInfo.FileName         = _ffmpeg;
        p.StartInfo.Arguments        = $"-i \"{path}\" -nostats -loglevel quiet -map 0:a -f null -";

        p.Start();
        p.WaitForExit();

        var result = p.ExitCode == 0;

        if (!result) _log.Error($"Invalid sound file: {fullPath}.");

        lock (_fileValidity)
        {
            return _fileValidity[fullPath] = result;
        }
    }

    private void Generate(
        IEnumerable<Sample> soundList, string workPath, string output)
    {
        var filter = new FilterComplex();

        // FIXME too slow, use binary search may faster
        Parallel.ForEach(soundList, sound =>
        {
            if (!CheckSoundValidity(sound.SoundFile, workPath))
            {
                return;
            }

            lock (filter) filter.AddFile(sound.SoundFile, sound.StartTime);
        });

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
            p.Start();
            p.WaitForExit();
        }
        finally
        {
            _lock.Release();
        }

        if (p.ExitCode != 0)
        {
            throw new Exception("generation failed");
        }

        File.Delete(argsFile);
    }

    public void GenerateMp3(List<Sample> allSoundList, string workPath, string output)
    {
        // ffmpeg can open at most 1300 files
        var groupSize = Math.Max(
            Math.Min((allSoundList.Count + _option.MaxThreads - 1) / _option.MaxThreads, 1300)
          , 10
        );

        var groupedSoundList = new List<(List<Sample> SoundList, string Output)>();

        var n = 0;

        // group sound file list by every X elements
        while (true)
        {
            var l = allSoundList.Skip(n++ * groupSize).Take(groupSize).ToList();
            if (!l.Any()) break;

            groupedSoundList.Add((l, Path.GetTempPath() + Guid.NewGuid() + ".mp3"));
        }

        // generate mp3 in parallel
        Parallel.ForEach(groupedSoundList, g =>
        {
            var startTime = g.SoundList.Min(x => x.StartTime);
            var endTime   = g.SoundList.Max(x => x.StartTime);

            _log.Info(
                $"{workPath}: Generating {TimeSpan.FromMilliseconds(startTime)}-{TimeSpan.FromMilliseconds(endTime)}...");

            Generate(g.SoundList, workPath, g.Output);
        });

        // merge temp mp3 files to result
        {
            _log.Info($"{workPath}: merging...");

            var soundList = groupedSoundList.Select(x => new Sample(0d, x.Output)).ToList();

            Generate(soundList, workPath, output);

            soundList.ForEach(l => File.Delete(l.SoundFile));
        }
    }
}