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
    private Dictionary<int, List<Signal>> TrackSignals { get; } = new();

    private List<Sample>? _songFileList;

    public Dictionary<int, List<HitObject>> HitObject { get; } = new()
    {
        { 0, new List<HitObject>() },
        { 1, new List<HitObject>() },
        { 2, new List<HitObject>() },
        { 3, new List<HitObject>() },
        { 4, new List<HitObject>() },
        { 5, new List<HitObject>() },
        { 6, new List<HitObject>() },
        { 7, new List<HitObject>() },
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

            if (line.WithCommand("#end", out _) && ignoreLine)
            {
                ignoreLine = false;
                continue;
            }

            if (line.WithCommand("#if", out var @if) && !ignoreLine)
            {
                if (string.IsNullOrEmpty(@if))
                {
                    ignoreLine = true;
                    continue;
                }

                if (@if[0] != '1')
                {
                    ignoreLine = true;
                }

                continue;
            }

            if (ignoreLine) continue;

            if (line.WithCommand("#Player", out var player))
            {
                if (player.IsEmpty())
                {
                    Log.Error($"{fp}: Player type cannot be determined. (Line: {i})");
                    throw new InvalidDataException();
                }

                switch (player[0])
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
            else if (line.WithCommand("#Title", out var title))
            {
                if (title.IsEmpty())
                {
                    Log.Warn($"{fp}: #Title is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.Title = title;
            }
            else if (line.WithCommand("#SubTitle", out var subTitle))
            {
                if (subTitle.IsEmpty())
                {
                    Log.Warn($"{fp}: #SubTitle is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.Subtitle = subTitle;
            }
            else if (line.WithCommand("#Artist", out var artist))
            {
                if (artist.IsEmpty())
                {
                    Log.Warn($"{fp}: #Artist is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.Artist = artist;
            }
            else if (line.WithCommand("#SubArtist", out var subArtist))
            {
                if (subArtist.IsEmpty())
                {
                    Log.Warn($"{fp}: #SubArtist is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.SubArtists.Add(subArtist);
            }
            else if (line.WithCommand("#Genre", out var genre))
            {
                if (genre.IsEmpty())
                {
                    Log.Warn($"{fp}: #genre is invalid, ignoring (Line: {i})");
                    metadata.Tags = "BMS";
                    continue;
                }

                metadata.Tags = genre;
            }
            else if (line.WithCommand("#LnObj", out var lnObj))
            {
                if (lnObj.IsEmpty())
                {
                    Log.Error($"{fp}: #LnObj is not a valid length (Line: {i})");
                    throw new InvalidDataException();
                }

                if (lnObj.Length != 2)
                {
                    Log.Error($"{fp}: #LnObj was specified, but not 2 bytes in length. (Line: {i})");
                    throw new InvalidDataException();
                }

                bms._lnObject = lnObj.ToLower();
            }
            else if (line.WithCommand("#PlayLevel", out var playLevel))
            {
                if (playLevel.IsEmpty())
                {
                    Log.Warn($"{fp}: #PlayLevel is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.Difficulty = playLevel;
            }
            else if (line.WithCommand("#StageFile", out var stageFile))
            {
                if (stageFile.IsEmpty())
                {
                    Log.Warn($"{fp}: #StageFile is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.StageFile = stageFile;
            }
            else if (line.WithCommand("#BACKBMP", out var backBmp))
            {
                if (backBmp.IsEmpty())
                {
                    Log.Warn($"{fp}: #BACKBMP is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.BackBmp = backBmp;
            }
            else if (line.WithCommand("#banner", out var banner))
            {
                if (banner.IsEmpty())
                {
                    Log.Warn($"{fp}: #banner is invalid, ignoring (Line: {i})");
                    continue;
                }

                metadata.Banner = banner;
            }
            else if (line.WithCommand("#bpm ", out var bpmDef))
            {
                if (bpmDef.IsEmpty())
                {
                    Log.Error($"{fp}: #bpm is invalid, aborting (Line: {i})");
                    throw new InvalidDataException();
                }

                if (double.TryParse(bpmDef, out var bpm))
                {
                    bms._startingBpm = bpm;
                }
                else
                {
                    Log.Error($"{fp}: #bpm {bpmDef} is invalid, aborting (Line: {i})");
                    throw new InvalidDataException();
                }
            }
            else if (line.WithCommand("#bpm", out var bpmChange))
            {
                var bpmChangeSplit = bpmChange.Split(' ').Where(c => !c.IsEmpty()).ToArray();

                if (bpmChangeSplit.Length != 2)
                {
                    Log.Error($"{fp}: BPM change {bpmChange} is invalid, aborting (Line: {i})");
                    throw new InvalidDataException();
                }

                if (double.TryParse(bpmChangeSplit[1], out var bpm))
                {
                    bms._indices.BpmChanges[bpmChangeSplit[0].ToLower()] = bpm;
                }
                else
                {
                    Log.Error($"{fp}: BPM change {bpmChange} is invalid, aborting (Line: {i})");
                    throw new InvalidDataException();
                }
            }
            else if (line.WithCommand("#bmp", out var bmp))
            {
                if (bmp.Length < 4)
                {
                    Log.Warn($"{fp}: BMP is invalid, ignoring (Line: {i})");
                    continue;
                }

                bms._indices.Bga[bmp[..2].ToLower()] = bmp[3..];
            }
            else if (line.WithCommand("#stop", out var stopDef))
            {
                var stopDefSplit = stopDef.Split(' ').Where(c => !c.IsEmpty()).ToArray();

                if (stopDefSplit.Length != 2)
                {
                    Log.Warn($"{fp}: STOP isn't correctly formatted, not going to use it (Line: {i})");
                    continue;
                }

                if (double.TryParse(stopDefSplit[1], out var stop))
                {
                    if (i < 0)
                    {
                        Log.Warn($"{fp}: STOP is negative ({stop}), not going to use it (Line: {i})");
                        continue;
                    }

                    bms._indices.Stops[stopDefSplit[0].ToLower()] = stop;
                }
                else
                {
                    Log.Warn($"{fp}: STOP is not a valid number, not going to use it (Line: {i})");
                }
            }
            else if (line.WithCommand("#wav", out var wav))
            {
                if (wav.Length < 4)
                {
                    Log.Warn(
                        $"{fp}: WAV command invalid, all notes/sfx associated with it won't be placed (Line: {i})");
                    continue;
                }

                bms._audioMap[wav[..2].ToLower()] = wav[3..];
            }
            else if (line.WithCommand("#", out var signalDef))
            {
                var signalDefSplit = signalDef.Split(':').Where(c => !c.IsEmpty()).ToArray();

                if (signalDefSplit.Length != 2) continue;
                if (signalDefSplit[0].Length != 5) continue;

                if (int.TryParse(signalDefSplit[0][..3], out var track))
                {
                    var channel = signalDefSplit[0][3..].ToLower();

                    var signal = new Signal
                    {
                        Channel = channel,
                        Message = signalDefSplit[1].ToLower()
                    };

                    if (!bms.TrackSignals.ContainsKey(track)) bms.TrackSignals[track] = new List<Signal>();

                    bms.TrackSignals[track].Add(signal);
                }
                else
                {
                    Log.Error($"{fp}: Failed to parse track #, cannot continue parsing (Line: {{i}}, Content: {line})");
                    throw new InvalidDataException();
                }
            }
        }

        bms.Metadata  = metadata;
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
        if (string.IsNullOrEmpty(hitObject.HitSoundFile))
        {
            Log.Debug($"{BmsPath}: found empty hit sound");
        }

        HitObject[lane].Add(hitObject);
    }

    #endregion

    public static BmsFileData FromFile(string fp)
    {
        var data = CompileBmsToObj(fp);

        if (data._audioMap.Count == 0)
        {
            Log.Error($"{fp}: The beatmap has no object, skipping...");
            throw new InvalidDataException();
        }

        var initBpm = data._startingBpm;
        data.TimingPoints[0] = initBpm;

        var startTrackAt = 0.0;

        // suppress warnings
        var bgaFrameWarn = new HashSet<string>();

        foreach (var track in Enumerable.Range(0, data.TrackSignals.Keys.Max() + 1))
        {
            if (!data.TrackSignals.ContainsKey(track)) data.TrackSignals[track] = new List<Signal>();

            var bpmChangeCollection = new BpmChangeCollection(
                track, data.TrackSignals[track], data._indices.BpmChanges, data._indices.Stops, fp
            );

            foreach (var signal in data.TrackSignals[track].Where(l => l.Message.Length % 2 == 0))
            {
                var lane = signal.GetLaneNumber();

                // not (note OR bga OR sound effect)
                if (!(lane != -1 || signal.Channel is "01" or "04" or "07"))
                {
                    continue;
                }

                for (var i = 0; i < signal.Message.Length; i += 2)
                {
                    var notePos = Timing.GetPosition(i, signal.Message.Length);
                    var message = signal.Message[i..(i + 2)];

                    if (message == "00") continue;

                    var offset = bpmChangeCollection.GetDurationBeforePosition(initBpm, notePos, false);

                    // Sound Effect
                    if (signal.Channel == "01")
                    {
                        data.AddSoundEffect(startTrackAt + offset, message);
                        continue;
                    }

                    // note
                    if (lane != -1)
                    {
                        // ln: type 1
                        if (message == data._lnObject)
                        {
                            var hitObj = data.HitObject[lane].Last();
                            hitObj.EndTime    = startTrackAt + offset;
                            hitObj.IsLongNote = true;
                            continue;
                        }

                        // ln: type 2
                        if (signal.Channel[0] == '5')
                        {
                            var hitObj = data.HitObject[lane].LastOrDefault(o => o.IsLongNote && o.EndTime == null);

                            // ln start
                            if (hitObj == null)
                            {
                                hitObj = new HitObject
                                {
                                    StartTime    = startTrackAt + offset,
                                    IsLongNote   = true,
                                    EndTime      = null,
                                    HitSoundFile = data._audioMap.ContainsKey(message) ? data._audioMap[message] : ""
                                };

                                data.AddHitObject(lane, hitObj);
                            }
                            // ln end
                            else
                            {
                                // update ln end time
                                hitObj.EndTime = startTrackAt + offset;

                                if (data._audioMap.ContainsKey(message))
                                {
                                    // ln end has different hit sound
                                    if (data._audioMap[message] != hitObj.HitSoundFile)
                                    {
                                        data.AddSoundEffect((double)hitObj.EndTime, message);
                                    }
                                }
                            }

                            continue;
                        }

                        // normal note
                        {
                            var hitObj = new HitObject
                            {
                                StartTime  = startTrackAt + offset,
                                IsLongNote = false
                            };

                            if (data._audioMap.ContainsKey(message))
                            {
                                hitObj.HitSoundFile = data._audioMap[message];
                            }

                            data.AddHitObject(lane, hitObj);
                        }

                        continue;
                    }

                    // bga
                    if (signal.Channel is "04" or "07")
                    {
                        if (!data._indices.Bga.ContainsKey(message))
                        {
                            // suppress warnings
                            if (!bgaFrameWarn.Contains(message))
                            {
                                Log.Warn($"{fp}: Bga frame {message} is not founded, ignoring...");
                                bgaFrameWarn.Add(message);
                            }

                            continue;
                        }

                        var bgaFile = data._indices.Bga[message];
                        var layer   = 0;

                        if (signal.Channel == "07") layer = 1;

                        if (bgaFile.Length > 0)
                        {
                            data.BgaFrames.Add(new BgaFrame
                            {
                                StartTime = startTrackAt + offset,
                                File      = bgaFile,
                                Layer     = layer
                            });
                        }
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

        if (data.HitObject.Values.All(x => !x.Any()) && !data.SoundEffects.Any())
        {
            Log.Error($"{fp}: The beatmap has no object, skipping...");
            throw new InvalidDataException();
        }

        data.BgaFrames.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
        return data;
    }

    public List<Sample> GetSoundFileList()
    {
        if (_songFileList != null) return _songFileList;

        _songFileList = new List<Sample>();

        _songFileList.AddRange(SoundEffects.Select(s => new Sample(s.StartTime, s.SoundFile)));
        _songFileList.AddRange(HitObject.Values.SelectMany(x => x).Select(x => new Sample(x.StartTime, x.HitSoundFile)));

        _songFileList = _songFileList
            .Where(l => !string.IsNullOrEmpty(l.SoundFile))
            .OrderBy(l => l.StartTime)
            .ThenBy(l => l.SoundFile)
            .ToList();

        return _songFileList;
    }
}