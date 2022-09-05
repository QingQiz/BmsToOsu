namespace BmsToOsu.Entity;

public class Sample
{
    public readonly double StartTime;
    public readonly string SoundFile;
    public static SampleEqualityComparer Comparer => new();

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

        // maybe 10ms is better?
        return Math.Abs(x.StartTime - y.StartTime) < 5 && x.SoundFile == y.SoundFile;
    }

    public int GetHashCode(Sample obj)
    {
        // should not combine obj.StartTime
        // IEnumerate.Intersect will first check `GetHashCode` and if not equal then `Equals` will not be called
        return obj.SoundFile.GetHashCode();
    }
}
