namespace BmsToOsu.Utils;

public static class PathExt
{
    public static Dictionary<string, string> FixSoundPath(this Dictionary<string, string> audioMap, string path)
    {
        var ext  = new[] { ".wav", ".mp3", ".ogg", ".3gp" };
        var dict = new Dictionary<string, string>();

        var files = audioMap.Values.ToHashSet();

        foreach (var f in files)
        {
            var prefix = Path.Join(path, Path.GetFileNameWithoutExtension(f));

            dict[f] = "";
            foreach (var e in ext)
            {
                var fp = prefix + e;

                if (!File.Exists(fp)) continue;

                dict[f] = Path.GetFileName(fp);
                break;
            }
        }

        return new Dictionary<string, string>(
            audioMap.Keys
                .Select(key => new KeyValuePair<string, string>(key, dict[audioMap[key]]))
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
        );
    }
}