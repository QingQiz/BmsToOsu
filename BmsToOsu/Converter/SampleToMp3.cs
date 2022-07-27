using System.Diagnostics;
using System.Text;
using BmsToOsu.Entity;
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

    public SampleToMp3(string ffmpeg = "")
    {
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
        p.StartInfo.Arguments = $"-i \"{path}\" -nostats -loglevel quiet -map 0:a -f null -";
        
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
        IEnumerable<(double StartTime, string SoundFile)> soundList, string workPath, string output)
    {
        var filter = new FilterComplex();

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

        p.Start();
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            throw new Exception("generation failed");
        }

        File.Delete(argsFile);
    }

    public void GenerateMp3(BmsFileData data, string output)
    {
        var allSoundList = data.GetSoundFileList();

        var groupSize = Math.Max(Math.Min(allSoundList.Count / 10, 1300), 10);

        var groupedSoundList = new List<(List<(double StartTime, string SoundFile)> SoundList, string Output)>();

        var n = 0;

        // group sound file list by every X elements
        while (true)
        {
            var l = allSoundList.Skip(n++ * groupSize).Take(groupSize).ToList();
            if (!l.Any()) break;

            groupedSoundList.Add((l, Path.GetTempPath() + Guid.NewGuid() + ".mp3"));
        }

        var workPath = Path.GetDirectoryName(data.BmsPath)!;

        // generate mp3 in parallel
        Parallel.ForEach(groupedSoundList, g =>
        {
            var startTime = g.SoundList.Min(x => x.StartTime);
            var endTime   = g.SoundList.Max(x => x.StartTime);

            _log.Info(
                $"{data.BmsPath}: Generating {TimeSpan.FromMilliseconds(startTime)}-{TimeSpan.FromMilliseconds(endTime)}...");

            Generate(g.SoundList, workPath, g.Output);
        });

        // merge temp mp3 files to result
        {
            _log.Info($"{data.BmsPath}: merging...");

            var soundList = groupedSoundList.Select(x => (0d, x.Output)).ToList();

            Generate(soundList, workPath, output);

            soundList.ForEach(l => File.Delete(l.Output));
        }
    }
}