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

    private bool ValidateSounds(List<string> soundName, string workPath)
    {
        if (!soundName.Any()) return true;

        soundName = soundName.Select(p => Path.Join(workPath, p)).ToList();

        IEnumerable<string> toTest;

        lock (FileValidity)
        {
            var tested = soundName.Where(p => FileValidity.ContainsKey(p));

            if (tested.Any(t => !FileValidity[t]))
            {
                return false;
            }

            toTest = soundName.Where(p => !FileValidity.ContainsKey(p));
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

        if (!result && soundName.Count == 1)
        {
            lock (FileValidity)
            {
                FileValidity[soundName[0]] = result;
            }
        }

        if (result)
        {
            lock (FileValidity)
            {
                soundName.ForEach(s => { FileValidity[s] = true; });
            }
        }

        return result;
    }

    public async Task<List<string>> CheckSoundValidity(List<string> soundName, string workPath)
    {
        var channel = Channel.CreateUnbounded<(int l, int r)>();

        channel.Writer.TryWrite((0, soundName.Count));

        var result = new List<string>();
        var tasks  = new List<Task>();
        var count  = 0;
        var @lock  = new object();

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

        return result;
    }
}