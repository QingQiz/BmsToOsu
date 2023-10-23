using System.Text;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Converter;

public static class Osu
{
    public static (string, HashSet<string> fileToCp) ToOsuBeatMap(
        this BmsFileData data, HashSet<Sample> excludingSamples, bool noSv, string mp3 = "", bool includePlate = false)
    {
        var dir = Path.GetDirectoryName(data.BmsPath)!;

        var fileToCp = new HashSet<string>();
        var bd       = new StringBuilder();
        var log      = LogManager.GetLogger(typeof(Osu));

        bd.AppendLine("osu file format v14\n");

        bd.AppendLine("[General]");
        bd.AppendLine("Mode: 3");
        bd.AppendLine("SampleSet: Soft");
        bd.AppendLine("Countdown: 0");

        if (!string.IsNullOrEmpty(mp3))
        {
            bd.AppendLine($"AudioFilename: {mp3}");
        }

        if (includePlate)
        {
            bd.AppendLine("SpecialStyle: 1");
        }

        bd.AppendLine("[Editor]");
        bd.AppendLine("DistanceSpacing: 1");
        bd.AppendLine("BeatDivisor: 1");
        bd.AppendLine("GridSize: 1");
        bd.AppendLine("TimelineZoom: 1");

        bd.AppendLine("[Metadata]");
        bd.AppendLine($"Title:{data.Metadata.Title}");
        bd.AppendLine($"Artist:{data.Metadata.Artist}");
        bd.AppendLine($"TitleUnicode:{data.Metadata.Title}");
        bd.AppendLine($"ArtistUnicode:{data.Metadata.Artist}");
        bd.AppendLine($"Creator:{StringExt.AppendSubArtist(data.Metadata.Artist, data.Metadata.SubArtists)}");
        bd.AppendLine("Source:BMS");
        bd.AppendLine($"Tags:{data.Metadata.Genre} BMS Converted");
        bd.AppendLine($"Version:{(noSv ? "[NSV]" : "")}Lv. {data.Metadata.Difficulty}");
        bd.AppendLine("BeatmapID:0");
        bd.AppendLine("BeatmapSetID:0");

        bd.AppendLine("[Difficulty]");
        bd.AppendLine("HPDrainRate:8.5");
        bd.AppendLine($"CircleSize:{(includePlate ? 8 : 7)}");
        bd.AppendLine("OverallDifficulty:8.0");
        bd.AppendLine("ApproachRate:0");
        bd.AppendLine("SliderMultiplier:1");
        bd.AppendLine("SliderTickRate:1");

        bd.AppendLine("[Events]");

        var bg = "";

        var imgExt = new[] { ".jpg", ".png", ".jpeg" };
        if (File.Exists(Path.Join(dir, data.Metadata.BackBmp)))
        {
            bg = data.Metadata.BackBmp;
        }
        else if (File.Exists(Path.Join(dir, data.Metadata.StageFile)))
        {
            bg = data.Metadata.StageFile;
        }
        else if (File.Exists(Path.Join(dir, data.Metadata.Banner)))
        {
            bg = data.Metadata.Banner;
        }
        else if (data.BgaFrames.Any())
        {
            var bga = data.BgaFrames
                .Where(x => imgExt
                    .Any(ext => x.File.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (bga.Any())
            {
                bg = bga[bga.Count / 2].File;
            }
        }

        if (string.IsNullOrEmpty(bg))
        {
            bg = Directory
                .GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => imgExt.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase))) ?? "";
        }

        if (!string.IsNullOrEmpty(bg))
        {
            bg = Path.GetFileName(bg);
            fileToCp.Add(bg);
            bd.AppendLine($"0,0,\"{bg.Escape()}\",0,0");
        }

        // bga
        for (var i = 0; i < data.BgaFrames.Count; i++)
        {
            var bga = data.BgaFrames[i];

            if (!File.Exists(Path.Join(dir, bga.File)))
            {
                continue;
            }

            var endTime = -1.0;

            for (var j = i + 1; j < data.BgaFrames.Count; j++)
            {
                if (data.BgaFrames[j].Layer == bga.Layer)
                {
                    endTime = data.BgaFrames[j].StartTime;
                    break;
                }
            }

            var vExt  = Path.GetExtension(bga.File);
            var layer = "Background";

            if (bga.Layer == 1)
            {
                layer = "Foreground";
            }

            if (vExt is not (".wmv" or ".mpg" or ".avi" or ".mp4" or ".webm" or ".mkv"))
            {
                bd.AppendLine($"Sprite,{layer},CentreRight,\"{bga.File.Escape()}\",600,240");
                bd.AppendLine($" F,0,{(int)bga.StartTime},{(int)endTime},1");
            }
            else
            {
                bd.AppendLine($"Video,{(int)bga.StartTime},\"{bga.File.Escape()}\"");
            }

            if (!string.IsNullOrEmpty(bga.File)) fileToCp.Add(bga.File);
        }

        var samples = new List<Sample>();

        // sound effect
        samples.AddRange(data.SoundEffects
            .Where(sample => !string.IsNullOrEmpty(sample.SoundFile))
            // ReSharper disable once PossibleUnintendedLinearSearchInSet
            // linear search is required
            .Where(sample => !excludingSamples.Contains(sample, Sample.Comparer))
        );

        // hit sound -> sound effect
        if (!string.IsNullOrEmpty(mp3))
        {
            samples.AddRange(data.HitObject.Values.SelectMany(h => h)
                .Where(hitObj => !string.IsNullOrEmpty(hitObj.HitSoundFile))
                .Select(hitObj => new Sample(hitObj.StartTime, hitObj.HitSoundFile))
                // ReSharper disable once PossibleUnintendedLinearSearchInSet
                // linear search is required
                .Where(sample => !excludingSamples.Contains(sample))
            );
        }
        if (!includePlate)
        {
            samples.AddRange(data.HitObject[0]
                .Where(hitObj => !string.IsNullOrEmpty(hitObj.HitSoundFile))
                .Select(hitObj => new Sample(hitObj.StartTime, hitObj.HitSoundFile))
                // ReSharper disable once PossibleUnintendedLinearSearchInSet
                // linear search is required
                .Where(sample => !excludingSamples.Contains(sample))
            );
        }

        foreach (var sample in samples.ToHashSet(Sample.Comparer).OrderBy(s => s.StartTime))
        {
            bd.AppendLine($"Sample,{(int)sample.StartTime},0,\"{sample.SoundFile.Escape()}\",100");
            fileToCp.Add(sample.SoundFile);
        }

        // timing points
        bd.AppendLine("[TimingPoints]");

        foreach (var ((j, k), i) in data.TimingPoints.Select((x, y) => (x, y)))
        {
            var val = Timing.BeatDuration(k);
            if (i == 0)
            {
                bd.AppendLine($"{j},{val},4,0,0,100,1,0");
                if (noSv) break;
            }
            else
            {
                if (val == 0)
                {
                    val = 999999999.0;
                }

                // if (val < 0)
                // {
                //     val = Math.Abs(val);
                // }

                bd.AppendLine($"{j},{val},4,0,0,100,1,0");
                bd.AppendLine($"{j},-100,4,0,0,100,0,0");
            }
        }

        // note/ln
        bd.AppendLine("[HitObjects]");

        var laneSize = 512.0 / (includePlate ? 8 : 7);

        foreach (var (lane, objects) in data.HitObject)
        {
            if (!includePlate && lane == 0) continue;

            var xPos = includePlate
                ? (int)Math.Floor(laneSize * lane + laneSize / 2)
                : (int)Math.Floor(laneSize * lane - laneSize / 2);

            var lastStartTime = -1;
            var lastEndTime   = -1;

            foreach (var obj in objects.OrderBy(o => o.StartTime))
            {
                var objType = 1 << 0;

                var ln = obj.IsLongNote && obj.EndTime != null;

                if (ln)
                {
                    objType = 1 << 7;
                }

                if (string.IsNullOrEmpty(obj.HitSoundFile)) continue;

                var startTime = (int)obj.StartTime;

                // double note at the same time
                if (startTime == lastStartTime)
                {
                    log.Error($"{data.BmsPath}: Double note at the same time.");
                    throw new InvalidDataException();
                }

                // note in ln
                if (startTime < lastEndTime)
                {
                    log.Error($"{data.BmsPath}: Note in Ln. abort.");
                    throw new InvalidDataException();
                }

                // ReSharper disable once PossibleUnintendedLinearSearchInSet
                // linear search is required
                var hitSound = string.IsNullOrEmpty(mp3)
                    ? excludingSamples.Contains(new Sample(obj.StartTime, obj.HitSoundFile), Sample.Comparer)
                        ? ""
                        : obj.HitSoundFile.Escape()
                    : "";

                if (!string.IsNullOrEmpty(hitSound))
                {
                    fileToCp.Add(obj.HitSoundFile);
                }

                var hitSoundVolume = string.IsNullOrEmpty(hitSound) ? 30 : 0;

                bd.AppendLine(ln
                    ? $"{xPos},192,{startTime},{objType},0,{(int)obj.EndTime!}:0:0:0:{hitSoundVolume}:{hitSound}"
                    : $"{xPos},192,{startTime},{1 << 0},0,0:0:0:{hitSoundVolume}:{hitSound}");

                lastStartTime = startTime;
                lastEndTime   = ln ? (int)obj.EndTime! : startTime;
            }
        }

        return (bd.ToString(), fileToCp);
    }
}