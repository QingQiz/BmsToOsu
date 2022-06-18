namespace BmsToOsu.Utils;

public static class StringExt
{
    public static string AppendSubArtist(string artist, IEnumerable<string> subArtists)
    {
        var sb = string.Join(" | ", subArtists);

        return string.IsNullOrEmpty(sb) ? artist : $"{artist} <{sb}>";
    }
}