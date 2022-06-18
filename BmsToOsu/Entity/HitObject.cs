namespace BmsToOsu.Entity;

public class HitObject
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public bool IsLongNote;
    public string HitSoundFile { get; set; } = "";
}