using System.Globalization;
using BmsToOsu.Entity;
using BmsToOsu.Utils;
using log4net;

namespace BmsToOsu.BpmChangeCalc;

public class BpmChangeCollection
{
    public readonly double MeasureScale = 1;
    public readonly List<BpmChange> BpmChanges = new();
    public readonly List<Stop> Stops = new();

    public BpmChangeCollection(
        int trackNo, IEnumerable<Line> lines, IReadOnlyDictionary<string, double> bpmChangeIndex,
        IReadOnlyDictionary<string, double> stopIndex)
    {
        var logger = LogManager.GetLogger(GetType())!;

        foreach (var line in lines)
        {
            switch (line.Channel)
            {
                case "02":
                {
                    if (double.TryParse(line.Message, out var i))
                    {
                        if (i <= 0)
                        {
                            logger.Error(
                                $"* Measure scale is negative or 0. cannot continue parsing (Track: {trackNo})");
                            throw new InvalidDataException();
                        }

                        MeasureScale = i;
                    }
                    else
                    {
                        logger.Error($"* Measure scale is invalid. cannot continue parsing (Track: {trackNo})");
                        throw new InvalidDataException();
                    }

                    continue;
                }
                case "03":
                case "08":
                {
                    if (line.Message.Length == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < line.Message.Length; i += 2)
                    {
                        if (i + 2 > line.Message.Length) continue;

                        var val = line.Message[i..(i + 2)];

                        if (val == "00") continue;

                        double bpm;

                        if (line.Channel == "03")
                        {
                            if (int.TryParse(val, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bpmP))
                            {
                                bpm = bpmP;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            if (bpmChangeIndex.ContainsKey(val))
                            {
                                bpm = bpmChangeIndex[val];
                            }
                            else
                                continue;
                        }

                        BpmChanges.Add(new BpmChange
                        {
                            Position   = Timing.GetPosition(i, line.Message.Length),
                            Bpm        = bpm,
                            IsNegative = bpm < 0
                        });
                    }

                    continue;
                }
                case "09":
                {
                    if (line.Message.Length == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < line.Message.Length; i += 2)
                    {
                        var val = line.Message[i..(i + 2)];

                        if (stopIndex.ContainsKey(val))
                        {
                            Stops.Add(new Stop
                            {
                                Position = Timing.GetPosition(i, line.Message.Length),
                                Duration = stopIndex[val]
                            });
                        }
                    }

                    continue;
                }
            }
        }

        BpmChanges = BpmChanges.OrderBy(c => c.Position).ToList();
        Stops      = Stops.OrderBy(s => s.Position).ToList();
    }

    public double GetDurationBeforePosition(double initBpm, double position)
    {
        var bpmChangesBeforePos = BpmChanges.Where(c => c.Position <= position).ToList();

        // no bpm change before position, return time before position + STOP time
        var totalDur = Timing.TrackDuration(initBpm, MeasureScale) * (position / 100.0);

        if (!bpmChangesBeforePos.Any()) return totalDur + GetStopDurationBeforePosition(initBpm, position);

        // otherwise, calculate the time before position using bpm change, and plus STOP time
        var bpm = initBpm;

        var timeBeforePos = Timing.TrackDuration(bpm, MeasureScale) * (bpmChangesBeforePos[0].Position / 100.0);

        for (var i = 0; i < bpmChangesBeforePos.Count; i++)
        {
            var tc = bpmChangesBeforePos[i];

            bpm = tc.Bpm;

            var t = Timing.TrackDuration(bpm, MeasureScale);

            if (i + 1 < bpmChangesBeforePos.Count)
            {
                timeBeforePos += t * ((bpmChangesBeforePos[i + 1].Position - tc.Position) / 100.0);
            }
            else
            {
                timeBeforePos += t * ((position - tc.Position) / 100.0);
            }
        }

        return timeBeforePos + GetStopDurationBeforePosition(initBpm, position);
    }

    /// <param name="initBpm"></param>
    /// <param name="pos">include the stop before pos</param>
    /// <returns>the total amount of time that all STOP before the current time would cause</returns>
    public double GetStopDurationBeforePosition(double initBpm, double pos)
    {
        if (Stops.Count == 0)
        {
            return 0;
        }

        var totalOffset = 0.0;

        foreach (var stop in Stops)
        {
            if (pos < stop.Position) continue;

            var bpmToUse = initBpm;

            foreach (var change in BpmChanges)
            {
                if (change.Position <= stop.Position)
                {
                    bpmToUse = change.Bpm;
                }
                else
                {
                    break;
                }
            }

            totalOffset += Timing.GetStopDuration(bpmToUse, stop.Duration);
        }

        return totalOffset;
    }

    public Dictionary<double, double> GetTimingPoints(double currentTime, double initBpm)
    {
        var points = new Dictionary<double, double>();

        // O(n^2), but easy to write
        foreach (var tc in BpmChanges)
        {
            var timeBeforeChange = GetDurationBeforePosition(initBpm, tc.Position);

            points[currentTime + timeBeforeChange] = tc.Bpm;
        }

        // O(n^2), but easy to write
        foreach (var stop in Stops)
        {
            var stopTime = GetStopDurationBeforePosition(initBpm, stop.Position);

            var timeBeforeStop = GetDurationBeforePosition(initBpm, stop.Position);

            points[currentTime + timeBeforeStop] = 0;
            points[currentTime + timeBeforeStop + stopTime] = BpmChanges
                .LastOrDefault(tc => tc.Position <= stop.Position)?.Bpm ?? initBpm;
        }

        return points;
    }

    public double TotalTrackDuration(double initBpm)
    {
        return GetDurationBeforePosition(initBpm, 100);
    }
}