namespace BmsToOsu.Entity;

public class HitObject
{
    public double StartTime { get; set; }
    public double? EndTime { get; set; }
    public bool IsLongNote { get; set; }
    public string HitSoundFile { get; set; } = "";

    private Sample? _sample;

    public Sample Sample
    {
        get => _sample ??= new Sample(StartTime, HitSoundFile);
        set => _sample = value;
    }
}