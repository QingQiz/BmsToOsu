namespace BmsToOsu.Entity;

public class AudioData
{
    public List<string> StringArray { get; set; } = new();
    public List<string> HexArray = new();

    public KeySound? GetHitSound(string target)
    {
        for (var i = 0; i < HexArray.Count; i++)
        {
            if (HexArray[i] == target)
            {
                return new KeySound
                {
                    Volume = 100,
                    Sample = i + 1
                };
            }
        }

        return null;
    }
}