using System.Text;
using BmsToOsu.BpmChangeCalc;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Entity;

public class BmsFileData
{
    public string BmsPath = "";
    public BmsMetadata Metadata = null!;
    private double _startingBpm = 130;
    private string _lnObject = "";
    private Dictionary<int, List<Line>> TrackLines { get; } = new();

    public Dictionary<int, List<HitObject>> HitObject { get; } = new()
    {
        {0, new List<HitObject>()},
        {1, new List<HitObject>()},
        {2, new List<HitObject>()},
        {3, new List<HitObject>()},
        {4, new List<HitObject>()},
        {5, new List<HitObject>()},
        {6, new List<HitObject>()},
        {7, new List<HitObject>()},
    };

    public Dictionary<double, double> TimingPoints { get; } = new();
    public List<BgaFrame> BgaFrames { get; } = new();

    // map audio reference to effect sound file name
    private Dictionary<string, string> _audioMap = new();
    private readonly IndexData _indices = new();
    public List<SoundEffect> SoundEffects { get; } = new();

    private static readonly ILog Log = LogManager.GetLogger(typeof(BmsFileData));

    #region Parser

    private static BmsFileData CompileBmsToObj(string fp)
    {
        var bytes = File.ReadAllBytes(fp);

        // ignore UTF-8 BOM
        // if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        // {
        //     bytes = bytes[3..];
        // }

        // ignore UTF-16 BOM
        // if (bytes[0] == 0xFF && bytes[1] == 0xFE)
        // {
        //     bytes = bytes[2..];
        // }

        // force Shift-JIS
        var       shiftJis = CodePagesEncodingProvider.Instance.GetEncoding("Shift-JIS")!;
        using var reader   = new StreamReader(new MemoryStream(bytes), shiftJis);
        var       lines    = reader.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var ignoreLine = false;

        var bms      = new BmsFileData();
        var metadata = new BmsMetadata();

        bms.BmsPath = fp;

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
                        Log.Error($"{fp}: Player type cannot be determined. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    switch (line[8])
                    {
                        case '1':
                            break;
                        case '2':
                            Log.Error($"{fp}: Map specified #PLAYER 2; skipping");
                            throw new InvalidDataException();
                        case '3':
                            Log.Error($"{fp}: Double play mode; skipping");
                            throw new InvalidDataException();
                        default:
                            Log.Error(
                                $"{fp}: Even though player header was defined, there was no valid input (Line: {i})");
                            throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#genre"))
                {
                    if (line.Length < 8)
                    {
                        Log.Warn($"{fp}: #genre is invalid, ignoring (Line: {i})");
                        metadata.Tags = "BMS";
                        continue;
                    }

                    metadata.Tags = line[7..];
                }
                else if (lower.StartsWith("#subtittle"))
                {
                    if (line.Length < 11)
                    {
                        Log.Warn($"{fp}: #subtitle is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Subtitle = line[10..];
                }
                else if (lower.StartsWith("#subartist"))
                {
                    if (line.Length < 12)
                    {
                        Log.Warn($"{fp}: #subartist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.SubArtists.Add(line[11..]);
                }
                else if (lower.StartsWith("#title"))
                {
                    if (line.Length < 8)
                    {
                        Log.Warn($"{fp}: #title is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Title = line[7..];
                }
                else if (lower.StartsWith("#lnobj"))
                {
                    if (line.Length < 8)
                    {
                        Log.Error($"{fp}: #lnobj is not a valid length (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (line[7..].Length != 2)
                    {
                        Log.Error($"{fp}: #lnobj was specified, but not 2 bytes in length. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    bms._lnObject = lower[7..];
                }
                else if (lower.StartsWith("#artist"))
                {
                    if (line.Length < 9)
                    {
                        Log.Warn($"{fp}: #artist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Artist = line[8..];
                }
                else if (lower.StartsWith("#playlevel"))
                {
                    if (line.Length < 12)
                    {
                        Log.Warn($"{fp}: #playlevel is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Difficulty = line[11..];
                }
                else if (lower.StartsWith("#stagefile"))
                {
                    if (line.Length < 12)
                    {
                        Log.Warn($"{fp}: #stagefile is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[11..];

                    metadata.StageFile = p;
                }
                else if (lower.StartsWith("#banner"))
                {
                    if (line.Length < 9)
                    {
                        Log.Warn($"{fp}: #banner is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[8..];

                    metadata.Banner = p;
                }
                else if (lower.StartsWith("#bpm "))
                {
                    if (line.Length < 6)
                    {
                        Log.Error($"{fp}: #bpm is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[5..], out var bpm))
                    {
                        bms._startingBpm = bpm;
                    }
                    else
                    {
                        Log.Error($"{fp}: #bpm {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bpm"))
                {
                    if (line.Length < 8)
                    {
                        Log.Error($"{fp}: BPM change is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[7..], out var bpm))
                    {
                        bms._indices.BpmChanges[lower[4..6]] = bpm;
                    }
                    else
                    {
                        Log.Error($"{fp}: BPM change {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bmp"))
                {
                    if (line.Length < 8)
                    {
                        Log.Warn($"{fp}: BMP is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[7..];

                    bms._indices.Bga[lower[4..6]] = p;
                }
                else if (lower.StartsWith("#stop"))
                {
                    if (line.Length < 9)
                    {
                        Log.Warn($"{fp}: STOP isn't correctly formatted, not going to use it (Line: {i})");
                        continue;
                    }

                    if (double.TryParse(line[8..], out var stop))
                    {
                        if (i < 0)
                        {
                            Log.Warn($"{fp}: STOP is negative ({stop}), not going to use it (Line: {i})");
                            continue;
                        }

                        bms._indices.Stops[lower[5..7]] = stop;
                    }
                    else
                    {
                        Log.Warn($"{fp}: STOP is not a valid number, not going to use it (Line: {i})");
                    }
                }
                else if (lower.StartsWith("#wav"))
                {
                    if (line.Length < 8)
                    {
                        Log.Warn(
                            $"{fp}: WAV command invalid, all notes/sfx associated with it won't be placed (Line: {i})");
                        continue;
                    }

                    bms._audioMap[lower[4..6]] = line[7..];
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
                Log.Error($"{fp}: Failed to parse track #, cannot continue parsing (Line: {{i}}, Content: {line})");
                throw new InvalidDataException();
            }
        }

        bms.Metadata = metadata;
        bms._audioMap = bms._audioMap.FixSoundPath(Path.GetDirectoryName(fp)!);
        return bms;
    }

    #endregion

    #region Help Functions

    private void AddSoundEffect(double time, string target)
    {
        if (_audioMap.ContainsKey(target))
        {
            SoundEffects.Add(new SoundEffect(time, _audioMap[target]));
        }
    }

    private void AddHitObject(int lane, HitObject hitObject)
    {
        if (!hitObject.IsLongNote)
        {
            HitObject[lane].Add(hitObject);
            return;
        }

        var lastObj = HitObject[lane].LastOrDefault();

        if (lastObj == null)
        {
            HitObject[lane].Add(hitObject);
            return;
        }

        if ((int)hitObject.StartTime == (int)lastObj.StartTime)
        {
            if (lastObj.HitSoundFile == hitObject.HitSoundFile)
            {
                lastObj.IsLongNote = hitObject.IsLongNote;
                lastObj.EndTime    = hitObject.EndTime;
            }
            else
            {
                // WARN double note at the same time
                HitObject[lane].Add(hitObject);
            }
        }
    }

    #endregion

    public static BmsFileData FromFile(string fp)
    {
        var data = CompileBmsToObj(fp);

        var initBpm = data._startingBpm;
        data.TimingPoints[0] = initBpm;

        var startTrackAt = 0.0;

        // suppress warnings
        var bgaFrameWarn = new HashSet<string>();

        foreach (var track in Enumerable.Range(0, data.TrackLines.Keys.Max() + 1))
        {
            if (!data.TrackLines.ContainsKey(track)) data.TrackLines[track] = new List<Line>();

            var bpmChangeCollection =
                new BpmChangeCollection(track, data.TrackLines[track], data._indices.BpmChanges, data._indices.Stops, fp);

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
                    if (line.Channel == "01")
                    {
                        data.AddSoundEffect(startTrackAt + localOffset, target);
                        continue;
                    }

                    // note
                    if (lane != -1)
                    {
                        // ln: type 1
                        if (target == data._lnObject)
                        {
                            var hitObj = data.HitObject[lane].Last();
                            hitObj.EndTime    = startTrackAt + localOffset;
                            hitObj.IsLongNote = true;
                            continue;
                        }

                        // ln: type 2
                        if (line.Channel[0] == '5')
                        {
                            var hitObj = data.HitObject[lane].LastOrDefault(o => o.IsLongNote && o.EndTime == null);

                            // ln start
                            if (hitObj == null)
                            {
                                hitObj = new HitObject
                                {
                                    StartTime    = startTrackAt + localOffset,
                                    IsLongNote   = true,
                                    EndTime      = null,
                                    HitSoundFile = data._audioMap.ContainsKey(target) ? data._audioMap[target] : ""
                                };

                                data.AddHitObject(lane, hitObj);
                            }
                            // ln end
                            else
                            {
                                // update ln end time
                                hitObj.EndTime = startTrackAt + localOffset;

                                if (data._audioMap.ContainsKey(target))
                                {
                                    // ln end has different hit sound
                                    if (data._audioMap[target] != hitObj.HitSoundFile)
                                    {
                                        data.AddSoundEffect((double)hitObj.EndTime, target);
                                    }
                                }
                            }
                            continue;
                        }

                        // normal note
                        {
                            var hitObj = new HitObject
                            {
                                StartTime  = startTrackAt + localOffset,
                                IsLongNote = false
                            };

                            if (data._audioMap.ContainsKey(target))
                            {
                                hitObj.HitSoundFile = data._audioMap[target];
                            }

                            data.AddHitObject(lane, hitObj);
                        }

                        continue;
                    }

                    // bga
                    if (line.Channel is "04" or "07")
                    {
                        if (!data._indices.Bga.ContainsKey(target))
                        {
                            // suppress warnings
                            if (!bgaFrameWarn.Contains(target))
                            {
                                Log.Warn($"{fp}: Bga frame {target} is not founded, ignoring...");
                                bgaFrameWarn.Add(target);
                            }

                            continue;
                        }

                        var t = data._indices.Bga[target];
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