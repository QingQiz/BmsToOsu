using System.Text;
using System.Text.RegularExpressions;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.Entity;

public class BmsFileData
{
    public BmsMetadata Metadata;
    public double StartingBpm = 130;
    public string LnObject;
    public readonly Dictionary<int, List<Line>> TrackLines = new();
    public readonly Dictionary<int, List<HitObject>> HitObject = new();
    public readonly Dictionary<double, double> TimingPoints = new();
    public List<BgaFrame> BgaFrames = new();
    public readonly AudioData AudioData = new();
    public readonly IndexData Indices = new();
    public List<SoundEffect> SoundEffects { get; set; } = new();

    private static BmsFileData CompileBmsToObj(string fp)
    {
        var logger = LogManager.GetLogger(typeof(BmsFileData))!;

        // Shift-JIS
        var lines = File.ReadAllLines(fp, CodePagesEncodingProvider.Instance.GetEncoding(932)!);

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
                        logger.Error($"* Player type cannot be determined. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    switch (line[8])
                    {
                        case '1':
                            break;
                        case '2':
                            logger.Error("* Map specified #PLAYER 2; skipping");
                            throw new InvalidDataException();
                        case '3':
                            logger.Error("* Double play mode; skipping");
                            throw new InvalidDataException();
                        default:
                            logger.Error(
                                $"* Even though player header was defined, there was no valid input (Line: {i})");
                            throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#genre"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"* #genre is invalid, ignoring (Line: {i})");
                        metadata.Tags = "BMS";
                        continue;
                    }

                    metadata.Tags = line[7..];
                }
                else if (lower.StartsWith("#subtittle"))
                {
                    if (line.Length < 11)
                    {
                        logger.Warn($"* #subtitle is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Subtitle = line[10..];
                }
                else if (lower.StartsWith("#subartist"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"* #subartist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.SubArtists.Add(line[11..]);
                }
                else if (lower.StartsWith("#title"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"* #title is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Title = line[7..];
                }
                else if (lower.StartsWith("#lnobj"))
                {
                    if (line.Length < 8)
                    {
                        logger.Error($"* #lnobj is not a valid length (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (line[7..].Length != 2)
                    {
                        logger.Error($"* #lnobj was specified, but not 2 bytes in length. (Line: {i})");
                        throw new InvalidDataException();
                    }

                    bms.LnObject = lower[7..];
                }
                else if (lower.StartsWith("#artist"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"* #artist is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Artist = line[8..];
                }
                else if (lower.StartsWith("#playlevel"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"* #playlevel is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Difficulty = line[11..];
                }
                else if (lower.StartsWith("#stagefile"))
                {
                    if (line.Length < 12)
                    {
                        logger.Warn($"* #stagefile is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[11..];

                    if (!File.Exists(Path.Join(Path.GetDirectoryName(fp), p)))
                    {
                        logger.Warn($"* \"{p}\" (#stagefile) wasn't found; ignoring (Line: {i})");
                        continue;
                    }

                    metadata.StageFile = p;
                }
                else if (lower.StartsWith("#banner"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"* #banner is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[8..];

                    if (!File.Exists(Path.Join(Path.GetDirectoryName(fp), p)))
                    {
                        logger.Warn($"* \"{p}\" (#banner) wasn't found; ignoring (Line: {i})");
                        continue;
                    }

                    metadata.Banner = p;
                }
                else if (lower.StartsWith("#bpm "))
                {
                    if (line.Length < 6)
                    {
                        logger.Error($"* #bpm is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[5..], out var bpm))
                    {
                        bms.StartingBpm = bpm;
                    }
                    else
                    {
                        logger.Error($"* #bpm {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bpm"))
                {
                    if (line.Length < 8)
                    {
                        logger.Error($"* BPM change is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }

                    if (double.TryParse(line[7..], out var bpm))
                    {
                        bms.Indices.BpmChanges[lower[4..6]] = bpm;
                    }
                    else
                    {
                        logger.Error($"* BPM change {line[5..]} is invalid, aborting (Line: {i})");
                        throw new InvalidDataException();
                    }
                }
                else if (lower.StartsWith("#bmp"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn($"* BMP is invalid, ignoring (Line: {i})");
                        continue;
                    }

                    var p = line[7..];

                    if (!File.Exists(Path.Join(Path.GetDirectoryName(fp), p)))
                    {
                        logger.Warn($"* \"{p}\" (BMP) wasn't found; ignoring (Line: {i})");
                        continue;
                    }

                    bms.Indices.Bga[line[4..6]] = p;
                }
                else if (lower.StartsWith("#stop"))
                {
                    if (line.Length < 9)
                    {
                        logger.Warn($"* STOP isn't correctly formatted, not going to use it (Line: {i})");
                        continue;
                    }

                    if (double.TryParse(line[8..], out var stop))
                    {
                        if (i < 0)
                        {
                            logger.Warn($"* STOP is negative ({stop}), not going to use it (Line: {i})");
                            continue;
                        }

                        bms.Indices.Stops[lower[5..7]] = stop;
                    }
                    else
                    {
                        logger.Warn($"* STOP is not a valid number, not going to use it (Line: {i})");
                        continue;
                    }
                }
                else if (lower.StartsWith("#wav"))
                {
                    if (line.Length < 8)
                    {
                        logger.Warn(
                            $"* WAV command invalid, all notes/sfx associated with it won't be placed (Line: {i})");
                        continue;
                    }

                    var soundName = PathExt.FindSoundFile(Path.GetDirectoryName(fp) ?? "", line[7..]);

                    if (soundName == null)
                    {
                        logger.Warn(
                            $"* (#WAV) \"{line[7..]}\" wasn't found or isn't either .wav, .mp3, .ogg, or .3gp. ignoring (Line: {i})");
                        continue;
                    }

                    bms.AudioData.StringArray.Add(soundName);
                    bms.AudioData.HexArray.Add(lower[4..6]);
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
                logger.Error($"* Failed to parse track #, cannot continue parsing (Line: {{i}}, Content: {line})");
                throw new InvalidDataException();
            }
        }

        bms.Metadata = metadata;
        return bms;
    }

    public static BmsFileData FromFile(string fp)
    {
        var logger = LogManager.GetLogger(typeof(BmsFileData))!;

        var data = CompileBmsToObj(fp);

        var initBpm = data.StartingBpm;
        data.TimingPoints[0] = initBpm;

        var noteRegex        = new Regex("[1][1-9]", RegexOptions.Compiled);
        var player2NoteRegex = new Regex("[2][1-9]", RegexOptions.Compiled);
        var lnRegex          = new Regex("[5][1-z]", RegexOptions.Compiled);
        var player2LnRegex   = new Regex("[6][1-9]", RegexOptions.Compiled);

        var base36Range = "0123456789abcdefghijklmnopqrstuvwxyz";

        var startTrackAt   = 0.0;
        var lnTracker      = new Dictionary<int, double>();
        var lnSoundTracker = new Dictionary<int, KeySound>();

        foreach (var track in data.TrackLines.Keys)
        {
            var localTrackData =
                new LocalTrackData(track, data.TrackLines[track], data.Indices.BpmChanges, data.Indices.Stops);

            foreach (var line in data.TrackLines[track].Where(l => l.Message.Length % 2 == 0))
            {
                if (player2NoteRegex.IsMatch(line.Channel) || player2LnRegex.IsMatch(line.Channel))
                {
                    logger.Error(
                        "* This map has notes in player 2's side, which would overlap player 1. Not going to process this map.");
                    throw new InvalidDataException();
                }

                if (!(noteRegex.IsMatch(line.Channel) || lnRegex.IsMatch(line.Channel) ||
                        line.Channel is "01" or "04" or "07"))
                {
                    continue;
                }

                for (var i = 0; i < line.Message.Length; i += 2)
                {
                    var notePos = Timing.GetPosition(i, line.Message.Length);
                    var target  = line.Message[i..(i + 2)];

                    if (target == "00") continue;

                    var localOffset = Timing.OffsetFromStartingTime(localTrackData, notePos, initBpm);
                    var sfx         = data.AudioData.GetHitSound(target);
                    var lane        = base36Range.IndexOf(line.Channel[1..], StringComparison.Ordinal);

                    if (noteRegex.IsMatch(line.Channel) || lnRegex.IsMatch(line.Channel))
                    {
                        if (lane != 6)
                        {
                            if (lane >= 8)
                            {
                                lane -= 2;
                            }

                            if (lane > 8)
                            {
                                logger.Error("* File wants more than 8 keys, skipping");
                                throw new InvalidDataException();
                            }

                            var hitObj = new HitObject
                            {
                                StartTime = startTrackAt + localOffset
                            };

                            if (target == data.LnObject)
                            {
                                // ln tail at the first object
                                if (data.HitObject[lane].Count == 0)
                                {
                                    continue;
                                }

                                var back = data.HitObject[lane].Count - 1;

                                // ln is too short
                                if (hitObj.StartTime - data.HitObject[lane][back].StartTime < 2.0)
                                {
                                    continue;
                                }

                                data.HitObject[lane][back].IsLongNote = true;
                                data.HitObject[lane][back].EndTime    = hitObj.StartTime;
                                continue;
                            }

                            if (sfx != null)
                            {
                                hitObj.KeySound = sfx;
                            }

                            if (lnRegex.IsMatch(line.Channel))
                            {
                                if (lnTracker.ContainsKey(lane) && lnTracker[lane] != 0)
                                {
                                    hitObj.EndTime    = hitObj.StartTime;
                                    hitObj.StartTime  = lnTracker[lane];
                                    hitObj.IsLongNote = true;

                                    if (lnSoundTracker.ContainsKey(lane))
                                    {
                                        hitObj.KeySound = new KeySound
                                        {
                                            Sample = lnSoundTracker[lane].Sample,
                                            Volume = lnSoundTracker[lane].Volume
                                        };
                                    }

                                    if (hitObj.EndTime <= hitObj.StartTime) continue;
                                }
                                else
                                {
                                    lnTracker[lane]      = hitObj.StartTime;
                                    lnSoundTracker[lane] = hitObj.KeySound!;
                                    continue;
                                }
                            }

                            if (!data.HitObject.ContainsKey(lane)) data.HitObject[lane] = new List<HitObject>();

                            data.HitObject[lane].Add(hitObj);
                        }
                    }

                    // lane == 6 means treat the key sound as sound effect
                    if (line.Channel == "01" || lane == 6)
                    {
                        var sound = new SoundEffect
                        {
                            StartTime = startTrackAt + localOffset
                        };

                        if (sfx != null)
                        {
                            sound.Sample = sfx.Sample;
                            sound.Volume = sfx.Volume;
                        }
                        else
                        {
                            continue;
                        }

                        data.SoundEffects.Add(sound);
                    }

                    if (line.Channel is "04" or "07")
                    {
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
                    }
                }
            }

            var fullLengthOfTrack = Timing.TotalTrackDuration(initBpm, localTrackData);

            var tp = Timing.TimingPoints(startTrackAt, initBpm, localTrackData);

            foreach (var k in tp)
            {
                data.TimingPoints[k.Key] = k.Value;
            }

            if (localTrackData.BpmChanges.Count > 0)
            {
                initBpm = localTrackData.BpmChanges.Last().Bpm;
            }

            startTrackAt += fullLengthOfTrack;

            data.TimingPoints[startTrackAt] = initBpm;
        }

        data.BgaFrames = data.BgaFrames.OrderBy(f => f.StartTime).ToList();

        return data;
    }
}