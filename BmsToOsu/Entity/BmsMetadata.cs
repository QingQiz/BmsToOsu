namespace BmsToOsu.Entity;

public class BmsMetadata
{
    public string Title = "";
    public string Artist = "";
    public string Tags = "";
    public string Difficulty = "";
    public string StageFile = "";
    public string Subtitle = "";
    public List<string> SubArtists { get; } = new();
    public string Banner = "";
}