using System.Text;
using BmsToOsu.Entity;
using BmsToOsu.Utils;

namespace BmsToOsu.Converter;

public static class Osu
{
    public static (string, HashSet<string> fileToCp) ToOsuBeatMap(this BmsFileData data)
    {
        var fileToCp = new HashSet<string>();
        var bd       = new StringBuilder();

        bd.AppendLine("osu file format v14\n");

        bd.AppendLine("[General]");
        bd.AppendLine("Mode: 3");
        bd.AppendLine("SampleSet: Soft");
        bd.AppendLine("Countdown: 0");

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
        bd.AppendLine($"Tags:{data.Metadata.Tags}");
        bd.AppendLine($"Version:Lv. {data.Metadata.Difficulty}");
        bd.AppendLine("BeatmapID:0");
        bd.AppendLine("BeatmapSetID:0");

        bd.AppendLine("[Difficulty]");
        bd.AppendLine("HPDrainRate:8.5");
        bd.AppendLine("CircleSize:7");
        bd.AppendLine("OverallDifficulty:8.0");
        bd.AppendLine("ApproachRate:0");
        bd.AppendLine("SliderMultiplier:1");
        bd.AppendLine("SliderTickRate:1");

        bd.AppendLine("[Events]");

        var bg = data.Metadata.StageFile;

        if (data.Metadata.Banner.Length > 0 && data.Metadata.StageFile.Length == 0)
        {
            bg = data.Metadata.Banner;
        }

        if (bg.Length != 0)
        {
            bd.AppendLine($"0,0,\"{bg}\",0,0");
        }

        for (var i = 0; i < data.BgaFrames.Count; i++)
        {
            var bga = data.BgaFrames[i];

            var endTime = 0.0;

            if (i + 1 != data.BgaFrames.Count)
            {
                endTime = data.BgaFrames[i + 1].StartTime;
            }

            var vExt  = Path.GetExtension(bga.File);
            var layer = "Background";

            if (bga.Layer == 1)
            {
                layer = "Foreground";
            }

            if (vExt is not (".wmv" or ".mpg" or ".avi" or ".mp4" or ".webm" or ".mkv"))
            {
                bd.AppendLine($"Sprite,{layer},CenterRight,\"{bga.File}\",600,240");
                bd.AppendLine($"_F,0,{(int)bga.StartTime},{(int)endTime},1");
            }
            else
            {
                bd.AppendLine($"Video,{(int)bga.StartTime},\"{bga.File}\"");
            }

            fileToCp.Add(bga.File);
        }

        foreach (var sfx in data.SoundEffects)
        {
            bd.AppendLine($"Sample,{(int)sfx.StartTime},0,\"{sfx.SoundFile}\",100");

            fileToCp.Add(sfx.SoundFile);
        }

        bd.AppendLine("[TimingPoints]");

        foreach (var ((j, k), i) in data.TimingPoints.Select((x, y) => (x, y)))
        {
            var val = Timing.BeatDuration(k);
            if (i == 0)
            {
                bd.AppendLine($"{j},{val},4,0,0,100,1,0");
            }
            else
            {
                if (val == 0)
                {
                    val = 999999999.0;
                }

                if (val < 0)
                {
                    val = Math.Abs(val);
                }

                bd.AppendLine($"{j},{val},4,0,0,100,1,0");
                bd.AppendLine($"{j},-100,4,0,0,100,0,0");
            }
        }

        bd.AppendLine("[HitObjects]");

        const double laneSize = 512.0 / 7;

        foreach (var (lane, objects) in data.HitObject)
        {
            var xPos = (int)Math.Floor(laneSize * lane - laneSize / 2);
            foreach (var obj in objects)
            {
                var objType = 1 << 0;

                if (obj.IsLongNote)
                {
                    objType = 1 << 7;
                }

                if (!obj.HitSoundFile.Any()) continue;

                fileToCp.Add(obj.HitSoundFile);

                bd.AppendLine(
                    obj.IsLongNote
                        ? $"{xPos},192,{(int)obj.StartTime},{objType},0,{(int)obj.EndTime}:0:0:0:0:{obj.HitSoundFile}"
                        : $"{xPos},192,{(int)obj.StartTime},{1 << 0},0,0:0:0:0:{obj.HitSoundFile}");
            }
        }

        return (bd.ToString(), fileToCp);
    }
}