using System.Diagnostics;
using System.Threading.Channels;

namespace BmsToOsu.Utils;

public class AudioValidator
{
    private readonly string _ffmpeg;
    private static readonly Dictionary<string, bool> FileValidity = new();

    public AudioValidator(string ffmpeg)
    {
        _ffmpeg = ffmpeg;
    }

    private bool ExpensiveValid(string soundName, string workPath)
    {
        using var p = new Process();

        p.StartInfo.UseShellExecute  = false;
        p.StartInfo.CreateNoWindow   = false;
        p.StartInfo.WorkingDirectory = workPath;
        p.StartInfo.FileName         = _ffmpeg;
        p.StartInfo.Arguments        = $"\"{Path.Join(workPath, soundName)}\" -nostats -loglevel quiet -map 0:a -f mp3 -";

        try
        {
            p.Start();
            p.WaitForExit();
        }
        catch
        {
            return false;
        }

        return p.ExitCode == 0;
    }

    private bool ValidateSounds(IReadOnlyCollection<string> soundName, string workPath)
    {
        if (!soundName.Any()) return true;

        var fullPath = soundName.Select(p => Path.Join(workPath, p)).ToList();

        IEnumerable<string> toTest;

        lock (FileValidity)
        {
            var tested = fullPath.Where(p => FileValidity.ContainsKey(p));

            if (tested.Any(t => !FileValidity[t]))
            {
                return false;
            }

            toTest = fullPath.Where(p => !FileValidity.ContainsKey(p));
        }

        var inputFiles = string.Join(' ', toTest.Select(Path.GetFileName).Select(p => $"-i \"{p}\""));

        using var p = new Process();

        p.StartInfo.UseShellExecute  = false;
        p.StartInfo.CreateNoWindow   = false;
        p.StartInfo.WorkingDirectory = workPath;
        p.StartInfo.FileName         = _ffmpeg;
        p.StartInfo.Arguments        = $"{inputFiles} -nostats -loglevel quiet -map 0:a -f null -";

        try
        {
            p.Start();
            p.WaitForExit();
        }
        catch
        {
            return false;
        }

        var result = p.ExitCode == 0;

        SetValidateResult(fullPath, result);

        return result;
    }
    
    private static void SetValidateResult(List<string> fullPath, bool result)
    {
        lock (FileValidity)
        {
            switch (result)
            {
                case false when fullPath.Count == 1:
                    FileValidity[fullPath[0]] = result;
                    break;
                case true:
                    fullPath.ForEach(s => { FileValidity[s] = true; });
                    break;
            }
        }
    }

    public async Task<HashSet<string>> CheckSoundValidity(List<string> soundName, string workPath)
    {
        soundName = soundName.Distinct().ToList();

        var channel = Channel.CreateUnbounded<(int l, int r)>();

        channel.Writer.TryWrite((0, soundName.Count));

        var result = new HashSet<string>();
        var tasks  = new List<Task>();
        var count  = 0;
        // lock count & result
        var @lock = new object();

        while (await channel.Reader.WaitToReadAsync())
        {
            var boundary = await channel.Reader.ReadAsync();

            tasks.Add(Task.Run(() =>
            {
                if (ValidateSounds(soundName.Skip(boundary.l).Take(boundary.r - boundary.l).ToList(), workPath))
                {
                    lock (@lock)
                    {
                        count += boundary.r - boundary.l;

                        if (count == soundName.Count) channel.Writer.Complete();
                    }
                }
                else
                {
                    if (boundary.r - boundary.l == 1)
                    {
                        lock (@lock)
                        {
                            count += 1;
                            if (count == soundName.Count) channel.Writer.Complete();
                            result.Add(Path.GetFileName(soundName[boundary.l]));
                        }
                    }
                    else
                    {
                        var mid = (boundary.r + boundary.l) / 2;
                        channel.Writer.TryWrite((boundary.l, mid));
                        channel.Writer.TryWrite((mid, boundary.r));
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        if (ExpensiveValid(soundName.Except(result).ToList().RandomTake(), workPath)) return result;

        SetValidateResult(soundName.Select(p => Path.Join(workPath, p)).ToList(), false);
        return soundName.ToHashSet();
    }
}