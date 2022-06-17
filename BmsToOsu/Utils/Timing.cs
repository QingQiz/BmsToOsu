using BmsToOsu.Entity;

namespace BmsToOsu.Utils;

public static class Timing
{
    /// <param name="index">track index</param>
    /// <param name="data"></param>
    /// <returns>how much time to add based on the BPM changes given for the track</returns>
    public static double BpmChangeOffset(int index, LocalTrackData data)
    {
        if (data.BpmChanges.Count == 0)
        {
            return 0;
        }

        var trackDuration = TrackDuration(data.BpmChanges[index].Bpm, data.MeasureScale);
        if (index + 1 < data.BpmChanges.Count)
        {
            return trackDuration *
                ((data.BpmChanges[index + 1].Position - data.BpmChanges[index].Position) / 100.0);
        }

        if (index + 1 == data.BpmChanges.Count)
        {
            return trackDuration * ((100.0 - data.BpmChanges[index].Position) / 100.0);
        }

        return 0;
    }

    /// <param name="initBpm"></param>
    /// <param name="pos"></param>
    /// <param name="data"></param>
    /// <returns>the total amount of time that all STOP before the current time would cause</returns>
    public static double StopOffset(double initBpm, double pos, LocalTrackData data)
    {
        if (data.Stops.Count == 0)
        {
            return 0;
        }

        var totalOffset = 0.0;

        foreach (var stop in data.Stops)
        {
            if (pos <= stop.Position) continue;

            var bpmToUse = initBpm;

            foreach (var (change, i) in data.BpmChanges.Select((v, i) => (v, i)))
            {
                // stop position is between bpm change i and bpm change i + 1, use i's bpm
                if ((i + 1 < data.BpmChanges.Count && data.BpmChanges[i + 1].Position > stop.Position &&
                        stop.Position >= change.Position) ||
                    (i + 1 == data.BpmChanges.Count && stop.Position >= change.Position))
                {
                    bpmToUse = change.Bpm;
                    break;
                }
            }

            totalOffset += StopDuration(bpmToUse, stop.Duration);
        }

        return totalOffset;
    }

    /// <param name="bpm"></param>
    /// <returns>the duration of a single beat of a track, in 4/4 meter, in milliseconds</returns>
    public static double BeatDuration(double bpm)
    {
        if (bpm == 0) return 0;

        return 60.0 / bpm * 1000;
    }

    /// <summary>
    /// This is assuming tracks are in 4/4 meter. I am still not sure if BMS songs can be in a different meter.
    /// </summary>
    /// <param name="bpm"></param>
    /// <returns>the beat duration for the current BPM * 4.</returns>
    public static double BaseTrackDuration(double bpm)
    {
        return BeatDuration(bpm) * 4.0;
    }

    /// <summary>
    /// STOP commands are based on 1/192 of a whole note in 4/4.
    /// </summary>
    /// <param name="bpm"></param>
    /// <param name="duration"></param>
    /// <returns>the duration that the track should remain at 0 BPM for</returns>
    public static double StopDuration(double bpm, double duration)
    {
        return BaseTrackDuration(bpm) * (duration / 192.0);
    }

    /// <param name="bpm"></param>
    /// <param name="measureScale"></param>
    /// <returns>the length of the track, in milliseconds, based on the BPM * the measure scale.</returns>
    public static double TrackDuration(double bpm, double measureScale)
    {
        return BaseTrackDuration(bpm) * measureScale;
    }

    public static double TotalTrackDuration(double initBpm, LocalTrackData data)
    {
        var baseLength = 0.0;

        if (data.BpmChanges.Count == 0 && data.Stops.Count == 0)
        {
            return TrackDuration(initBpm, data.MeasureScale);
        }

        foreach (var (change, i) in data.BpmChanges.Select((v, i) => (v, i)))
        {
            if (i == 0)
            {
                baseLength += TrackDuration(initBpm, data.MeasureScale) * (change.Position / 100.0);
            }

            baseLength += BpmChangeOffset(i, data);
        }

        if (data.BpmChanges.Count == 0)
        {
            baseLength += TrackDuration(initBpm, data.MeasureScale);
        }

        var stopTime = StopOffset(initBpm, 100.0, data);

        return baseLength + stopTime;
    }

    public static double GetPosition(int i, int length)
    {
        if (length == 2)
        {
            return 0;
        }

        return (double)(i / 2.0) / (length / 2.0) * 100.0;
    }

    public static double OffsetFromStartingTime(LocalTrackData data, double notePos, double initBpm)
    {
        if (data.BpmChanges.Count == 0 && data.Stops.Count == 0)
        {
            return TrackDuration(initBpm, data.MeasureScale) * (notePos / 100.0);
        }

        var timeToAdd = 0.0;

        foreach (var (t, i) in data.BpmChanges.Select((t, i) => (t, i)))
        {
            if (i == 0)
            {
                var d = TrackDuration(initBpm, data.MeasureScale);
                if (notePos < t.Position)
                {
                    timeToAdd += d * (notePos / 100.0);
                    break;
                }
                else
                {
                    timeToAdd += d * (t.Position / 100.0);
                }
            }

            var trackDuration = TrackDuration(t.Bpm, data.MeasureScale);

            if ((i + 1 == data.BpmChanges.Count && notePos >= t.Position) ||
                (i + 1 < data.BpmChanges.Count && data.BpmChanges[i + 1].Position > notePos && notePos >= t.Position))
            {
                timeToAdd += trackDuration * ((notePos - t.Position) / 100.0);
                break;
            }
            else if (i + 1 < data.BpmChanges.Count)
            {
                timeToAdd += trackDuration * ((data.BpmChanges[i + 1].Position - t.Position) / 100.0);
            }
        }

        if (data.BpmChanges.Count == 0)
        {
            timeToAdd += TrackDuration(initBpm, data.MeasureScale) * (notePos / 100.0);
        }

        if (data.Stops.Count == 0)
        {
            timeToAdd += StopOffset(initBpm, notePos, data);
        }

        return timeToAdd;
    }

    public static Dictionary<double, double> TimingPoints(double currentTime, double initBpm, LocalTrackData data)
    {
        var points = new Dictionary<double, double>();

        if (data.BpmChanges.Count > 0)
        {
            var timeElapsed = 0.0;

            for (var i = 0; i < data.BpmChanges.Count; i++)
            {
                var tc = data.BpmChanges[i];

                if (i == 0)
                {
                    timeElapsed += TrackDuration(initBpm, data.MeasureScale);
                }

                var stopTime = StopOffset(initBpm, tc.Position, data);

                var bpm = tc.IsNegative ? 0 : tc.Bpm;

                points[currentTime + stopTime + timeElapsed] = bpm;

                timeElapsed += BpmChangeOffset(i, data);
            }
        }

        if (data.Stops.Count > 0)
        {
            var timeElapsed = 0.0;

            double stopTime;

            foreach (var (stop, stopIdx) in data.Stops.Select((s, i) => (s, i)))
            {
                if (data.BpmChanges.Count > 0)
                {
                    var localTimeElapsed = 0.0;

                    stopTime = StopOffset(initBpm, stop.Position, data);

                    for (var i = 0; i < data.BpmChanges.Count; i++)
                    {
                        var tc = data.BpmChanges[i];

                        var t = TrackDuration(initBpm, data.MeasureScale);

                        if (i == 0)
                        {
                            localTimeElapsed += t * (tc.Position / 100.0);
                        }

                        if ((i + 1 < data.BpmChanges.Count && data.BpmChanges[i + 1].Position > stop.Position &&
                                stop.Position >= tc.Position) ||
                            (i + 1 == data.BpmChanges.Count && stop.Position >= tc.Position))
                        {
                            var startAt = currentTime + localTimeElapsed + stopTime +
                                t * ((stop.Position - tc.Position) / 100.0);
                            var endAt = startAt + StopDuration(tc.Bpm, stop.Duration);

                            points[startAt] = 0;
                            points[endAt]   = tc.Bpm;
                            break;
                        }
                        else if (i + 1 == data.BpmChanges.Count && stop.Position < data.BpmChanges[0].Position)
                        {
                            var startAt = currentTime + stopTime + t * (stop.Position / 100.0);
                            var endAt   = startAt + StopDuration(initBpm, stop.Duration);

                            points[startAt] = 0;
                            points[endAt]   = tc.Bpm;
                            break;
                        }

                        localTimeElapsed += BpmChangeOffset(i, data);
                    }

                    continue;
                }

                if (stopIdx == 0)
                {
                    timeElapsed += TrackDuration(initBpm, data.MeasureScale) * (stop.Position / 100.0);
                }

                stopTime = StopOffset(initBpm, stop.Position, data);

                points[currentTime + timeElapsed + stopTime]                                        = 0;
                points[currentTime = timeElapsed + stopTime + StopDuration(initBpm, stop.Duration)] = initBpm;

                if (stopIdx + 1 < data.Stops.Count)
                {
                    timeElapsed += TrackDuration(initBpm, data.MeasureScale) *
                        ((data.Stops[stopIdx + 1].Position - stop.Position) / 100.0);
                }

                if (stopIdx + 1 == data.Stops.Count)
                {
                    timeElapsed += TrackDuration(initBpm, data.MeasureScale) * ((100.0 - stop.Position) / 100.0);
                }
            }
        }

        return points;
    }
}