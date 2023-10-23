namespace BmsToOsu.Utils;

public static class StringExt
{
    public static string AppendSubArtist(string artist, IEnumerable<string> subArtists)
    {
        var sb = string.Join(" | ", subArtists);

        return string.IsNullOrEmpty(sb) ? artist : $"{artist} <{sb}>";
    }

    public static bool WithCommand(this string s, string command, out string param)
    {
        if (s.StartsWith(command, StringComparison.OrdinalIgnoreCase))
        {
            param = s[command.Length..].Trim();
            return true;
        }

        param = "";
        return false;
    }

    public static bool IsEmpty(this string s)
    {
        return s.Length == 0;
    }

    public static string MostCommonPrefix(this IList<string> strings)
    {
        return new string(strings.MinBy(s => s.Length)?
            .TakeWhile((c, i) => strings.All(s => s[i] == c)).ToArray());
    }
}