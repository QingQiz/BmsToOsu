namespace BmsToOsu.Utils;

public class Sample
{
    public readonly double StartTime;
    public readonly string SoundFile;
    public static readonly SampleEqualityComparer Comparer = new();

    public Sample(double startTime, string soundFile)
    {
        StartTime = startTime;
        SoundFile = soundFile;
    }
}

public class SampleEqualityComparer : IEqualityComparer<Sample>
{
    public bool Equals(Sample? x, Sample? y)
    {
        if (x == null || y == null) return false;

        return Math.Abs(x.StartTime - y.StartTime) < 0.1 && x.SoundFile == y.SoundFile;
    }

    public int GetHashCode(Sample obj)
    {
        return HashCode.Combine(obj.StartTime, obj.SoundFile);
    }
}