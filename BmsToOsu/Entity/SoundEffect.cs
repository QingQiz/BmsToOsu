namespace BmsToOsu.Entity;

public class SoundEffect
{
    public readonly double StartTime;
    public readonly string SoundFile;

    public SoundEffect(double time, string file)
    {
        StartTime = time;
        SoundFile = file;
    }
}