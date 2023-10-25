using System.Runtime.InteropServices;

namespace BmsToOsu.Utils;

public static class PathExt
{
#if _WINDOWS
    [DllImport("Shlwapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool PathFileExists(StringBuilder path);
#endif

    public static bool FileExists(string path)
    {
#if _WINDOWS
        var builder = new StringBuilder();
        builder.Append(path);
        return PathFileExists(builder);
#else
        return File.Exists(path);
#endif
    }

    public static Dictionary<string, string> FixSoundPath(this Dictionary<string, string> audioMap, string path)
    {
        var ext  = new[] { ".ogg", ".wav", ".mp3", ".3gp" };
        var dict = new Dictionary<string, string>();

        var files = audioMap.Values.ToHashSet();

        foreach (var f in files)
        {
            var prefix = Path.Join(path, Path.GetFileNameWithoutExtension(f));

            dict[f] = "";
            foreach (var e in ext)
            {
                var fp = prefix + e;


                if (!FileExists(fp)) continue;

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

    public static string Escape(this string path)
    {
        return path.Replace(",", "-comma-");
    }

    public static string MakeValidFileName(this string name)
    {
        var invalidChars =
            System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }
}