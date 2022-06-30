using System.Text;
using BmsToOsu.BpmChangeCalc;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Entity;

public class BmsFileData
{
    public BmsMetadata Metadata = null!;
    public double StartingBpm = 130;
    public string LnObject = "";
    public Dictionary<int, List<Line>> TrackLines { get; } = new();
    public Dictionary<int, List<HitObject>> HitObject { get; } = new();
    public Dictionary<double, double> TimingPoints { get; } = new();
    public List<BgaFrame> BgaFrames { get; } = new();

    // map audio reference to effect sound file name
    public Dictionary<string, string> AudioMap { get; set; } = new();
    public IndexData Indices { get; } = new();
    public List<SoundEffect> SoundEffects { get; } = new();

    #region Parser

    private static BmsFileData CompileBmsToObj(string fp)
    {
        var logger = LogManager.GetLogger(typeof(BmsFileData))!;

        var bytes = File.ReadAllBytes(fp);

        // ignore UTF-8 BOM
        if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }

        // ignore UTF-16 BOM
        if (bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            bytes = bytes[2..];
        }

        // force Shift-JIS
        var       shiftJis = CodePagesEncodingProvider.Instance.GetEncoding("Shift-JIS")!;
        using var reader   = new StreamReader(new MemoryStream(bytes), shiftJis);
        var       lines    = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var ignoreLine = false;

        var bms      = new BmsFileData();
        var metadata = new BmsMetadata();

        foreach (var (line, i) in lines.Select((v, i) => (v, i)))
        {
            if (!line.StartsWith('#')) continue;

            var lower = line.ToLower();

            if (lower.StartsWith("#end") && ignoreLine)
            {
                ignoreLine = false;
                continue;
            }

            if (lower.StartsWith("#if") && !ignoreLine)
            {
                if (line.Length < 5)
                {
                    ignoreLine = true;
                    continue;
                }

                if (line[4] != '1')
                {
                    ignoreLine = true;
                }

                continue;
            }

            if (ignoreLine) continue;

            if (line.Length < 7 || line[6] != ':')
            {
                if (lower.StartsWith("#player"))
                {
                    if (line.Length < 9)
                    {
                        logger.Error($"{fp}: Player type cannot be determined. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    switch (line[8])
                    {
                        case '1':
                            break;
                        case '2':
                            logger.Error($"{fp}: Map specified #PLAYER 2; skipping");
                            throw new InvalidDataException();
                        case '3':
                            logger.Error($"{fp}: Double play mode; skipping");
                            throw new InvalidDataException();
                        default:
                            logger.Error(
                                $"{fp}: Even though player header was defined, there was no valid input (Line: {i})");
                            throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#genre"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"{fp}: #genre is invalid, ignoring (Line: {i})");
                        metadata.Tags = "BMS";
                        continue;
                    }

                    metadata.Tags = line[7..];
                }
                else if (lower.StartsWith("#subtittle"))
                {
                    if (line.Length < 11)
                    {
                        logger.Warn($"{fp}: #subtitle is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Subtitle = line[10..];
                }
                else if (lower.StartsWith("#subartist"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"{fp}: #subartist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.SubArtists.Add(line[11..]);
                }
                else if (lower.StartsWith("#title"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"{fp}: #title is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Title = line[7..];
                }
                else if (lower.StartsWith("#lnobj"))
                {
                    if (line.Length < 8)
                    {
                        logger.Error($"{fp}: #lnobj is not a valid length (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (line[7..].Length != 2)
                    {
                        logger.Error($"{fp}: #lnobj was specified, but not 2 bytes in length. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    bms.LnObject = lower[7..];
                }
                else if (lower.StartsWith("#artist"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"{fp}: #artist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Artist = line[8..];
                }
                else if (lower.StartsWith("#playlevel"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"{fp}: #playlevel is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Difficulty = line[11..];
                }
                else if (lower.StartsWith("#stagefile"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"{fp}: #stagefile is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[11..];

                    metadata.StageFile = p;
                }
                else if (lower.StartsWith("#banner"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"{fp}: #banner is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[8..];

                    metadata.Banner = p;
                }
                else if (lower.StartsWith("#bpm "))
                {
                    if (line.Length < 6)
                    {
                        logger.Error($"{fp}: #bpm is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[5..], out var bpm))
                    {
                        bms.StartingBpm = bpm;
                    }
                    else
                    {
                        logger.Error($"{fp}: #bpm {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bpm"))
                {
                    if (line.Length < 8)
                    {
                        logger.Error($"{fp}: BPM change is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[7..], out var bpm))
                    {
                        bms.Indices.BpmChanges[lower[4..6]] = bpm;
                    }
                    else
                    {
                        logger.Error($"{fp}: BPM change {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bmp"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"{fp}: BMP is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[7..];

                    bms.Indices.Bga[lower[4..6]] = p;
                }
                else if (lower.StartsWith("#stop"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"{fp}: STOP isn't correctly formatted, not going to use it (Line: {i})");
                        continue;
                    }

                    if (double.TryParse(line[8..], out var stop))
                    {
                        if (i < 0)
                        {
                            logger.Warn($"{fp}: STOP is negative ({stop}), not going to use it (Line: {i})");
                            continue;
                        }

                        bms.Indices.Stops[lower[5..7]] = stop;
                    }
                    else
                    {
                        logger.Warn($"{fp}: STOP is not a valid number, not going to use it (Line: {i})");
                    }
                }
                else if (lower.StartsWith("#wav"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn(
                            $"{fp}: WAV command invalid, all notes/sfx associated with it won't be placed (Line: {i})");
                        continue;
                    }

                    bms.AudioMap[lower[4..6]] = line[7..];
                }

                continue;
            }

            if (int.TryParse(line[1..4], out var tInt))
            {
                var channel = lower[4..6];

                var lineData = new Line
                {
                    Channel = channel
                };

                if (line.Length > 7)
                {
                    lineData.Message = lower[7..];
                }

                if (!bms.TrackLines.ContainsKey(tInt)) bms.TrackLines[tInt] = new List<Line>();

                bms.TrackLines[tInt].Add(lineData);
            }
            else
            {
                logger.Error($"{fp}: Failed to parse track #, cannot continue parsing (Line: {{i}}, Content: {line})");
                throw new InvalidDataException();
            }
        }

        bms.Metadata = metadata;
        bms.AudioMap = bms.AudioMap.FixSoundPath(Path.GetDirectoryName(fp)!);
        return bms;
    }

    #endregion

    #region Help Functions

    private void AddSoundEffect(double time, string target)
    {
        if (AudioMap.ContainsKey(target))
        {
            SoundEffects.Add(new SoundEffect(time, AudioMap[target]));
        }
    }

    #endregion

    public static BmsFileData FromFile(string fp)
    {
        var logger = LogManager.GetLogger(typeof(BmsFileData))!;

        var data = CompileBmsToObj(fp);

        var initBpm = data.StartingBpm;
        data.TimingPoints[0] = initBpm;

        var startTrackAt = 0.0;

        // suppress warnings
        var bgaFrameWarn = new HashSet<string>();

        foreach (var track in Enumerable.Range(0, data.TrackLines.Keys.Max() + 1))
        {
            if (!data.TrackLines.ContainsKey(track)) data.TrackLines[track] = new List<Line>();

            var bpmChangeCollection =
                new BpmChangeCollection(track, data.TrackLines[track], data.Indices.BpmChanges, data.Indices.Stops, fp);

            foreach (var line in data.TrackLines[track].Where(l => l.Message.Length % 2 == 0))
            {
                var lane = line.GetLaneNumber();

                // not (note OR bga OR sound effect)
                if (!(lane != -1 || line.Channel is "01" or "04" or "07"))
                {
                    continue;
                }

                for (var i = 0; i < line.Message.Length; i += 2)
                {
                    var notePos = Timing.GetPosition(i, line.Message.Length);
                    var target  = line.Message[i..(i + 2)];

                    if (target == "00") continue;

                    var localOffset = bpmChangeCollection.GetDurationBeforePosition(initBpm, notePos, false);

                    // Sound Effect
                    if (lane == 0 || line.Channel == "01")
                    {
                        data.AddSoundEffect(startTrackAt + localOffset, target);
                        continue;
                    }

                    // note
                    if (lane != -1)
                    {
                        // ln
                        if (target == data.LnObject)
                        {
                            var hitObj = data.HitObject[lane].Last();
                            hitObj.EndTime    = startTrackAt + localOffset;
                            hitObj.IsLongNote = true;
                        }
                        // normal note
                        else
                        {
                            var hitObj = new HitObject
                            {
                                StartTime  = startTrackAt + localOffset,
                                IsLongNote = false
                            };
                            if (data.AudioMap.ContainsKey(target))
                            {
                                hitObj.HitSoundFile = data.AudioMap[target];
                            }

                            if (!data.HitObject.ContainsKey(lane)) data.HitObject[lane] = new List<HitObject>();

                            data.HitObject[lane].Add(hitObj);
                        }

                        continue;
                    }

                    if (line.Channel is "04" or "07")
                    {
                        if (!data.Indices.Bga.ContainsKey(target))
                        {
                            // suppress warnings
                            if (!bgaFrameWarn.Contains(target))
                            {
                                logger.Warn($"{fp}: Bga frame {target} is not founded, ignoring...");
                                bgaFrameWarn.Add(target);
                            }
                            continue;
                        }

                        var t = data.Indices.Bga[target];
                        var l = 0;

                        if (line.Channel == "07") l = 1;

                        if (t.Length > 0)
                        {
                            data.BgaFrames.Add(new BgaFrame
                            {
                                StartTime = startTrackAt + localOffset,
                                File      = t,
                                Layer     = l
                            });
                        }

                        continue;
                    }
                }
            }

            var fullLengthOfTrack = bpmChangeCollection.TotalTrackDuration(initBpm);

            var tp = bpmChangeCollection.GetTimingPoints(startTrackAt, initBpm);

            foreach (var k in tp)
            {
                data.TimingPoints[k.Key] = k.Value;
            }

            if (bpmChangeCollection.BpmChanges.Count > 0)
            {
                initBpm = bpmChangeCollection.BpmChanges.Last().Bpm;
            }

            startTrackAt += fullLengthOfTrack;
        }

        data.BgaFrames.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        return data;
    }
}