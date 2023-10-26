namespace BmsToOsu.Entity;

public record Sample(double StartTime, string SoundFile)
{
    public static SampleEqualityComparer Comparer => new();

    public bool Valid => !string.IsNullOrEmpty(SoundFile);
}

public class SampleEqualityComparer : IEqualityComparer<Sample>
{
    public bool Equals(Sample? x, Sample? y)
    {
        if (x is null || y is null) return false;

        // maybe 10ms is better?
        return Math.Abs(x.StartTime - y.StartTime) < Constants.MaxSampleOffsetError && x.SoundFile == y.SoundFile;
    }

    public int GetHashCode(Sample obj)
    {
        // should not combine obj.StartTime
        // IEnumerate.Intersect will first check `GetHashCode` and if not equal then `Equals` will not be called
        return obj.SoundFile.GetHashCode();
    }
}
