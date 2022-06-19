namespace BmsToOsu.Utils;

public static class Timing
{
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
    public static double GetStopDuration(double bpm, double duration)
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

    public static double GetPosition(int i, int length)
    {
        if (length == 2)
        {
            return 0;
        }

        return i / 2.0 / (length / 2.0) * 100.0;
    }
}